using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GmTool.Pages.Chart
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

            //list of department  
            lstModel = new List<SimpleReportViewModel>();
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = "Technology",
                Quantity = rnd.Next(10)
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = "Sales",
                Quantity = rnd.Next(10)
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = "Marketing",
                Quantity = rnd.Next(10)
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = "Human Resource",
                Quantity = rnd.Next(10)
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = "Research and Development",
                Quantity = rnd.Next(10)
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = "Acconting",
                Quantity = rnd.Next(10)
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = "Support",
                Quantity = rnd.Next(10)
            });
            lstModel.Add(new SimpleReportViewModel
            {
                DimensionOne = "Logistics",
                Quantity = rnd.Next(10)
            });
        }
    }
}
