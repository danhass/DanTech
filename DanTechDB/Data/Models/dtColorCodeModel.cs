using System.Diagnostics.CodeAnalysis;
using DanTech.Data;

namespace DanTech.Data.Models
{
    public class dtColorCodeModel
    {
        public dtColorCodeModel() { }
        public dtColorCodeModel (dtColorCode colorCode)
        {
            id = colorCode.id;
            title = colorCode.title;
            note = colorCode.note;
        }
        public int id { get; set; }
        [AllowNull]
        public string title { get; set; }
        [AllowNull]
        public string note { get; set; }
    }
}
