using System;
using CsvHelper.Configuration.Attributes;

namespace Reble.RKIWebService.Entities
{
	public class HospitalizationData
	{
		[Index(0)] public DateTime Date { get; set; }
		[Index(1)] public string? StateName { get; set; }
		[Index(2)] public StateIds StateId { get; set; }
		[Index(3)] public string? AgeGroup { get; set; }
		[Index(4)] public int Hospitalization7TCases { get; set; }
		[Index(5)] public double Hospitalization7TIncidence { get; set; }
	}
}