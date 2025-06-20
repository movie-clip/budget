using System;
using System.Collections.Generic;
using HomeCharts.Models;
using HomeCharts.Models.Budget;

namespace HomeCharts.ViewModels
{
    public static class ImportTemplate
    {
        public static List<BudgetTransaction> GetExpenses()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();
            //January 2022
            result.AddRange(AddJanuary());
            result.AddRange(AddFeb());
            result.AddRange(AddMarch());
            result.AddRange(AddApril());
            result.AddRange(AddMay());
            result.AddRange(AddJune());
            result.AddRange(AddJuly());
            result.AddRange(AddAugust());
            result.AddRange(AddSept());
            result.AddRange(AddOct());
            
            return result;
        }

        private static List<BudgetTransaction> AddJanuary()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Cafe,
                Value = 160
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Grocery,
                Value = 550
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Rent,
                Value = 1000
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Smoke,
                Value = 210
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Transport,
                Value = 110
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Shopping,
                Value = 1225
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Travels,
                Value = 600
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Family,
                Value = 200
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Sasha,
                Value = 240
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Medicine,
                Value = 1200
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 1, 1),
                Category = BudgetCategory.Cosmetic,
                Value = 20
            });

            return result;
        }

        private static List<BudgetTransaction> AddFeb()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 2, 1),
                Category = BudgetCategory.Cafe,
                Value = 100
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 2, 1),
                Category = BudgetCategory.Grocery,
                Value = 360
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 2, 1),
                Category = BudgetCategory.Rent,
                Value = 1380
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 2, 1),
                Category = BudgetCategory.Smoke,
                Value = 210
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 2, 1),
                Category = BudgetCategory.Transport,
                Value = 100
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 2, 1),
                Category = BudgetCategory.Shopping,
                Value = 430
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 2, 1),
                Category = BudgetCategory.Travels,
                Value = 1000
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 2, 1),
                Category = BudgetCategory.Family,
                Value = 360
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 2, 1),
                Category = BudgetCategory.Sasha,
                Value = 35
            });

            return result;
        }

        private static List<BudgetTransaction> AddMarch()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 3, 1),
                Category = BudgetCategory.Cafe,
                Value = 0
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 3, 1),
                Category = BudgetCategory.Grocery,
                Value = 420
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 3, 1),
                Category = BudgetCategory.Rent,
                Value = 1100
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 3, 1),
                Category = BudgetCategory.Smoke,
                Value = 200
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 3, 1),
                Category = BudgetCategory.Transport,
                Value = 10
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 3, 1),
                Category = BudgetCategory.Shopping,
                Value = 80
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 3, 1),
                Category = BudgetCategory.Sasha,
                Value = 35
            });

            return result;
        }

        private static List<BudgetTransaction> AddApril()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 4, 1),
                Category = BudgetCategory.Cafe,
                Value = 200
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 4, 1),
                Category = BudgetCategory.Grocery,
                Value = 400
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 4, 1),
                Category = BudgetCategory.Rent,
                Value = 1030
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 4, 1),
                Category = BudgetCategory.Smoke,
                Value = 260
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 4, 1),
                Category = BudgetCategory.Transport,
                Value = 60
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 4, 1),
                Category = BudgetCategory.Shopping,
                Value = 245
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 4, 1),
                Category = BudgetCategory.Travels,
                Value = 235
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 4, 1),
                Category = BudgetCategory.Sasha,
                Value = 35
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 4, 1),
                Category = BudgetCategory.Medicine,
                Value = 35
            });

            return result;
        }

        private static List<BudgetTransaction> AddMay()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 5, 1),
                Category = BudgetCategory.Cafe,
                Value = 310
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 5, 1),
                Category = BudgetCategory.Grocery,
                Value = 640
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 5, 1),
                Category = BudgetCategory.Rent,
                Value = 1120
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 5, 1),
                Category = BudgetCategory.Smoke,
                Value = 290
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 5, 1),
                Category = BudgetCategory.Transport,
                Value = 160
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 5, 1),
                Category = BudgetCategory.Shopping,
                Value = 1700
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 5, 1),
                Category = BudgetCategory.Travels,
                Value = 900
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 5, 1),
                Category = BudgetCategory.Family,
                Value = 700
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 5, 1),
                Category = BudgetCategory.Sasha,
                Value = 700
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 5, 1),
                Category = BudgetCategory.Medicine,
                Value = 40
            });

            return result;
        }

        private static List<BudgetTransaction> AddJune()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 6, 1),
                Category = BudgetCategory.Cafe,
                Value = 550
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 6, 1),
                Category = BudgetCategory.Grocery,
                Value = 650
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 6, 1),
                Category = BudgetCategory.Rent,
                Value = 940
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 6, 1),
                Category = BudgetCategory.Smoke,
                Value = 200
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 6, 1),
                Category = BudgetCategory.Transport,
                Value = 250
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 6, 1),
                Category = BudgetCategory.Shopping,
                Value = 1000
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 6, 1),
                Category = BudgetCategory.Travels,
                Value = 750
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 6, 1),
                Category = BudgetCategory.Family,
                Value = 500
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 6, 1),
                Category = BudgetCategory.Medicine,
                Value = 20
            });

            return result;
        }

        private static List<BudgetTransaction> AddJuly()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 7, 1),
                Category = BudgetCategory.Cafe,
                Value = 310
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 7, 1),
                Category = BudgetCategory.Grocery,
                Value = 680
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 7, 1),
                Category = BudgetCategory.Rent,
                Value = 1140
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 7, 1),
                Category = BudgetCategory.Smoke,
                Value = 115
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 7, 1),
                Category = BudgetCategory.Transport,
                Value = 180
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 7, 1),
                Category = BudgetCategory.Shopping,
                Value = 1100
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 7, 1),
                Category = BudgetCategory.Travels,
                Value = 800
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 7, 1),
                Category = BudgetCategory.Family,
                Value = 300
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 7, 1),
                Category = BudgetCategory.Sasha,
                Value = 32
            });

            return result;
        }

        private static List<BudgetTransaction> AddAugust()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Cafe,
                Value = 710
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Grocery,
                Value = 570
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Rent,
                Value = 1050
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Smoke,
                Value = 255
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Transport,
                Value = 480
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Shopping,
                Value = 170
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Travels,
                Value = 1000
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Family,
                Value = 300
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Education,
                Value = 250
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Sasha,
                Value = 32
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 8, 1),
                Category = BudgetCategory.Cosmetic,
                Value = 37
            });

            return result;
        }

        private static List<BudgetTransaction> AddSept()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 9, 1),
                Category = BudgetCategory.Cafe,
                Value = 230
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 9, 1),
                Category = BudgetCategory.Grocery,
                Value = 530
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 9, 1),
                Category = BudgetCategory.Rent,
                Value = 1210
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 9, 1),
                Category = BudgetCategory.Smoke,
                Value = 240
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 9, 1),
                Category = BudgetCategory.Transport,
                Value = 270
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 9, 1),
                Category = BudgetCategory.Shopping,
                Value = 205
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 9, 1),
                Category = BudgetCategory.Travels,
                Value = 300
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 9, 1),
                Category = BudgetCategory.Family,
                Value = 575
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 9, 1),
                Category = BudgetCategory.Sasha,
                Value = 35
            });

            return result;
        }

        private static List<BudgetTransaction> AddOct()
        {
            List<BudgetTransaction> result = new List<BudgetTransaction>();

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 10, 1),
                Category = BudgetCategory.Cafe,
                Value = 450
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 10, 1),
                Category = BudgetCategory.Grocery,
                Value = 540
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 10, 1),
                Category = BudgetCategory.Rent,
                Value = 970
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 10, 1),
                Category = BudgetCategory.Smoke,
                Value = 230
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 10, 1),
                Category = BudgetCategory.Transport,
                Value = 220
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 10, 1),
                Category = BudgetCategory.Shopping,
                Value = 530
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 10, 1),
                Category = BudgetCategory.Family,
                Value = 280
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 10, 1),
                Category = BudgetCategory.Sasha,
                Value = 200
            });

            result.Add(new BudgetTransaction
            {
                Date = new DateTime(2022, 10, 1),
                Category = BudgetCategory.Medicine,
                Value = 65
            });

            return result;
        }
    }
}