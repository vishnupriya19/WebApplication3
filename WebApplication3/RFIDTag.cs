using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;


namespace WebApplication3
{
    public class RFIDTag : Controller
    {
        public string ID { get; set; }
        public string Type { get; set; }
        public string Battery { get; set; }
        public string Temperature { get; set; }
        public string Vibration { get; set; }
        public string Call { get; set; }
        public string Buckle { get; set; }
        public string Mount { get; set; }
        public string ButtonPress { get; set; }
    }
    public class BLEBeacon
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Time { get; set; }
        public string Mac { get; set; }
        public string Name { get; set; }
        public string iBUUIDMajorMinor { get; set; }
        public string EsNSInstanceID { get; set; }
        public string Rssi { get; set; }
        public string Battery { get; set; }
        public string Temperature { get; set; }
        public string Humidity { get; set; }
        public string Vibration { get; set; }
        public string ButtonPressed { get; set; }
        public int Count { get; set; }

    }
}
