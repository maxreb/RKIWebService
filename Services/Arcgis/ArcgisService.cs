using Reble.RKIWebService.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Reble.RKIWebService.Services.Arcgis
{
	public class ArcgisServiceOptions
	{
		public const string OptionsPath = "Arcgis";
		public int MaxDataSets { get; set; } = 365;
		public string DatabasePath { get; set; } = "data/arcgis/";
	}
	public sealed class ArcgisService : BackgroundService, ICovidApiService
	{
		ArcgisServiceOptions Options { get; }

		private readonly ILogger<ArcgisService> _logger;

		private readonly CityDataset _datasetCity;
		private readonly StateDataset _datasetState;


		byte[]? oldHash;

		public ArcgisService(ILogger<ArcgisService> logger,
			IConfiguration configuration,
			IHttpClientFactory? http = null)
		{
			_logger = logger;
			var httpClient = http?.CreateClient() ?? new HttpClient();
			_datasetState = new StateDataset(httpClient);
			_datasetCity = new CityDataset(httpClient);

			Options = new ArcgisServiceOptions();
			configuration.GetSection(ArcgisServiceOptions.OptionsPath).Bind(Options);



		}

		private string GetDBPath(Dataset dataset)
		{
			string res = Path.Combine(Options.DatabasePath, dataset.GetType().Name.ToLower());
			if (!Directory.Exists(res))
				Directory.CreateDirectory(res);
			return res;
		}


		private void ReadDatabase(Dataset dataset)
		{
			try
			{
				var dbPath = GetDBPath(dataset);
				var files = Directory.GetFiles(dbPath, "*.json");
				foreach (var file in files)
				{
					var json = File.ReadAllText(file);
					if (!dataset.SetCurrentDataSet(json))
					{
						_logger.LogWarning($"File {json} is empty.");
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ReadDatabase error");
			}
		}
		private void ReadDatabases()
		{

			_logger.LogInformation("Read databases...");
			if (!Directory.Exists(Options.DatabasePath))
			{
				Directory.CreateDirectory(Options.DatabasePath);
				return;
			}
			ReadDatabase(_datasetCity);
			ReadDatabase(_datasetState);
			_logger.LogInformation("Database: {0} city and {0} state records found", _datasetCity.PastData.Count, _datasetState.PastData.Count);
			CleanUpDatabase();
		}

		//This will remove the oldest datasets when there are
		//more then maxNumOfDataSets (default: 14) from data file location
		private void CleanUpDatabase()
		{
			cleanup(_datasetCity);
			cleanup(_datasetState);

			void cleanup(Dataset dataset)
			{

				string dir = GetDBPath(dataset);
				var files = Directory.GetFiles(dir, "*.json");
				//Cleanup files
				if (files.Length > Options.MaxDataSets)
				{
					_logger.LogInformation("Clean up database, delete {0} files...", files.Length - Options.MaxDataSets);
					var filesToDelete = files.OrderBy(f => f).Take(files.Length - Options.MaxDataSets);
					foreach (var file in filesToDelete)
					{
						_logger.LogDebug("Delete {file}", Path.GetFileName(file));
						File.Delete(file);
					}
				}
				//Cleanup mem
				if (dataset.PastData.Count > Options.MaxDataSets)
				{
					_logger.LogInformation("Clean up memory, delete {0} datasets...", dataset.PastData.Count - Options.MaxDataSets);
					var rm = dataset.PastData.Keys.Take(dataset.PastData.Count - Options.MaxDataSets);
					foreach (var r in rm)
						dataset.PastData.Remove(r);
				}
			}
		}
		private async Task<bool> GetNewData(Dataset dataset)
		{
			try
			{
				var json = await dataset.QueryJson();
				if (string.IsNullOrEmpty(json))
				{
					_logger.LogError("QueryJson failed, json is null");
					return false;
				}

				if (dataset.SetCurrentDataSet(json, _logger))
				{
					string dbPath = GetDBPath(dataset);
					string fileName = dataset.LastUpdate.ToString("yyyyMMdd") + ".json";
					string filePath = Path.Combine(dbPath, fileName);
					File.WriteAllText(filePath, json);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetNewData failed");
				return false;
			}
			return true;
		}

		private async Task CheckForNewData()
		{
			try
			{
				using SHA256 hash = SHA256.Create();
				string json = await _datasetCity.QueryFromCityKeyJson("01002");


				var newHash = hash.ComputeHash(Encoding.UTF8.GetBytes(json));
				//Previously we did this by comparing the LastDate's but 
				//we had to implement it with hash as a failsafe because the 
				//RKI sometimes uploads wrong data and then refreshes
				//them without notifying/with the same date
				if (oldHash == null || !newHash.SequenceEqual(oldHash))
				{
					_logger.LogInformation("New updated arrived");
					bool success = await GetNewData(_datasetCity);//for cities
					success &= await GetNewData(_datasetState);//for federal states
					if (success)
						oldHash = newHash;
					CleanUpDatabase();
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "CheckForNewData failed");
			}
		}

		public ICovid19Data? GetCurrentCityData(string cityKey) => _datasetCity.GetCurrent(cityKey);
		public ICovid19Data? GetCurrentStateData(string cityKey) => _datasetState.GetCurrent(cityKey);

		public bool TryGetCityData(string cityKey, DateTime from, out IEnumerable<ICovid19Data> data, DateTime? to = null)
			=> _datasetCity.TryGetFromCityKey(cityKey, from, out data, to);

		public bool TryGetStateData(string cityKey, DateTime from, out IEnumerable<ICovid19Data> data, DateTime? to = null)
		=> _datasetState.TryGetFromCityKey(cityKey, from, out data, to);

		public bool TryGetCountryData(DateTime @from, out IEnumerable<ICovid19Data> data, DateTime? to = null)
		{
			throw new NotImplementedException();
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			ReadDatabases();
			while (!stoppingToken.IsCancellationRequested)
			{
				await CheckForNewData();
				await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
			}
		}
	}
}
