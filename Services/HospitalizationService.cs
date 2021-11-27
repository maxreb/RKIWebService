using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Hosting;
using Reble.RKIWebService.Entities;

namespace Reble.RKIWebService.Services
{
	public class HospitalizationService : BackgroundService
	{
		private readonly HttpClient _httpClient;

		private const string RequestUri =
			"/robert-koch-institut/COVID-19-Hospitalisierungen_in_Deutschland/master/Aktuell_Deutschland_COVID-19-Hospitalisierungen.csv";

		private Dictionary<StateIds, IEnumerable<HospitalizationData>> _records =
			new Dictionary<StateIds, IEnumerable<HospitalizationData>>();

		public TaskCompletionSource<bool> TaskCompletionSource { get; set; } = new TaskCompletionSource<bool>();


		public HospitalizationService(IHttpClientFactory factory)
		{
			_httpClient = factory.CreateClient();
			_httpClient.BaseAddress = new Uri("https://raw.githubusercontent.com/");
		}


		public IEnumerable<HospitalizationData> GetStateRecordsByCityKey(string cityKey, DateTime from)
		{
			var stateId = CitiesRepository.GetStateFromCityKey(cityKey);
			return GetStateRecordsByStateId(stateId, from);
		}

		public IEnumerable<HospitalizationData> GetStateRecordsByStateId(StateIds stateId, DateTime from)
		{
			_records.TryGetValue(stateId, out var records);
			return records?.Where(x => x.Date >= from) ?? new HospitalizationData[] { };
		}

		public HospitalizationData GetRecordsByDateAndState(DateTime date, StateIds stateId)
		{
			return _records[stateId].Single(x => x.Date == date);
		}


		private async Task GetHospitalizationData()
		{
			var stream = await _httpClient.GetStreamAsync(RequestUri);
			var reader = new StreamReader(stream);
			using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture));
			var mapper = csv.Context.AutoMap<HospitalizationData>();


			mapper.Map(t => t.Hospitalization7TCases).TypeConverter(new TryInt32Converter());
			mapper.Map(t => t.Hospitalization7TIncidence).TypeConverter(new TryDoubleConverter());


			_records = csv.GetRecords<HospitalizationData>()
				.Where(data => data.AgeGroup == "00+")
				.OrderBy(data => data.Date)
				.GroupBy(data => data.StateId)
				.ToDictionary(data => data.Key, data => (IEnumerable<HospitalizationData>)data);
			TaskCompletionSource.SetResult(true);
		}


		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await GetHospitalizationData();

				//get the utc timespan from tomorrow at 5 am
				var tomorrow = DateTime.UtcNow.AddDays(1);
				var timeSpan = new TimeSpan(5, 0, 0);
				var nextRun = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
				var timeToWait = nextRun - DateTime.UtcNow;
				await Task.Delay(timeToWait, stoppingToken);
			}
		}
	}

	public class TryInt32Converter : Int32Converter
	{
		public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
		{
			return int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int result) ? result : 0;
		}
	}

	public class TryDoubleConverter : DoubleConverter
	{
		public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
		{
			return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double result) ? result : 0;
		}
	}
}