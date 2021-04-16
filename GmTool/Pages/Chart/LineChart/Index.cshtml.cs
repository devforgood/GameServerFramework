using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GmTool.Pages.Chart.LineChart
{
    public class SimpleReportViewModel
    {
        public string DimensionOne { get; set; }
        public int Quantity { get; set; }
    }

    public class IndexModel : PageModel
    {
        public string StackedDimensionOne { get; set; }
        public List<SimpleReportViewModel> lstModel { get; set; }
        private Random rnd = new Random();
        public void OnGet()
        {
            var current = DateTime.UtcNow;

            //list of countries  
            lstModel = new List<SimpleReportViewModel>();
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = current.AddMinutes(-5).ToString(),
                Quantity = rnd.Next(10000) + 10000
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = current.AddMinutes(-4).ToString(),
                Quantity = rnd.Next(10000) + 10000
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = current.AddMinutes(-3).ToString(),
                Quantity = rnd.Next(10000) + 10000
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = current.AddMinutes(-2).ToString(),
                Quantity = rnd.Next(10000) + 10000
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = current.AddMinutes(-1).ToString(),
                Quantity = rnd.Next(10000) + 10000
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = current.ToString(),
                Quantity = rnd.Next(10000) + 10000
            });
        }
    }
}
