using DanTech.Data;
using DanTech.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTNutrition.Services
{
    public class DTNutritionService
    {
        IDTDBDataService? _db = null;

        public DTNutritionService() { }
        public DTNutritionService(IDTDBDataService db)
        {
            _db = db;
        }

        public dtFood? Set (dtFood food)
        {
            dtFood? returnVal = null;
            if (food == null || string.IsNullOrEmpty(food.title)) return returnVal;
            try
            {
                var existing = _db.Foods.Where(x => x.title == food.title).FirstOrDefault();
                if (existing != null) return returnVal;
                returnVal = _db.Set(food);
            } 
            catch (Exception)
            {
                return returnVal;
            }
            return returnVal;
        }
    }
}
