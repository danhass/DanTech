using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using DanTech.Data;

namespace DTNutrition.Services
{
    public interface IDTNutritionService
    {
        dtFood? Set(dtFood food);
    }
}
