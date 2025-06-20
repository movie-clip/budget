using System;
using FileHelpers;

namespace HomeCharts.Models.Budget
{
    [IgnoreEmptyLines]
    [DelimitedRecord("|")]
    public class Record
    {
        [FieldConverter(ConverterKind.Date, "dd/MM/yyyy")]
        public DateTime DateTime;
        public string Vendor;
        [FieldConverter(ConverterKind.Date, "dd/MM/yyyy")]
        public DateTime TransactionDateTime;
        public float Value;
        public float Balance;
        public string Card;
        public string Temp;
    }
}