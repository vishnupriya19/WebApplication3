using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Filter;
using WebApplication3.Wrappers;

namespace WebApplication3.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("[controller]")]
    public class RFIDController : Controller
    {
        private readonly ILogger<RFIDController> _logger;
        private MySqlConnection connection;
        public RFIDController(ILogger<RFIDController> logger, MySqlConnection connection)
        {
            _logger = logger;
            this.connection = connection;
        }

        [HttpGet]
        public async Task<PagedResponse<List<BLEBeacon>>> Get([FromQuery(Name = "employee-id")] string? employeeId, [FromQuery(Name = "start-date")] string? startDate, [FromQuery(Name = "end-date")] string? endDate, [FromQuery(Name = "pageNumber")] int pageNumber=0, [FromQuery(Name = "pageSize")] int pageSize=0)
        {
            var validFilter = new PaginationFilter(pageNumber, pageSize);
            var limit = validFilter.PageSize;
            var offset = (validFilter.PageNumber - 1) * validFilter.PageSize;
            List<BLEBeacon> Beacons = new List<BLEBeacon>();
            await connection.OpenAsync();

            using var command = new MySqlCommand("select * from BLEBeacon b, User_table u where u.MAC = b.MAC and (@employeeId IS NULL OR u.emp_id = @employeeId) and (@startDate IS NULL OR b.Time >= @startDate) and (@endDate IS NULL OR b.Time <= @endDate) limit @limit offset @offset", connection);
            command.Parameters.Add(new MySqlParameter("employeeId", employeeId));
            command.Parameters.Add(new MySqlParameter("startDate", startDate));
            command.Parameters.Add(new MySqlParameter("endDate", endDate));
            command.Parameters.Add(new MySqlParameter("limit", limit));
            command.Parameters.Add(new MySqlParameter("offset", offset));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var value = reader.GetValue(0);
                BLEBeacon beacon = new BLEBeacon();
                beacon.EmployeeId = reader["emp_id"].ToString();
                beacon.EmployeeName = reader["name"].ToString();
                beacon.Mac = reader["Mac"].ToString();
                beacon.Name = reader["Tag_Name"].ToString();
                beacon.iBUUIDMajorMinor = reader["iBUUIDMajorMinor"].ToString();
                beacon.EsNSInstanceID = reader["EsNSInstanceID"].ToString();
                beacon.Rssi = reader["Rssi"].ToString();
                beacon.Battery = reader["Battery"].ToString();
                beacon.Temperature = reader["Temperature"].ToString();
                beacon.Humidity = reader["Humidity"].ToString();
                beacon.Vibration = reader["Vibration"].ToString();
                beacon.ButtonPressed = reader["ButtonPressed"].ToString();
                beacon.Time = reader["Time"].ToString();
                Beacons.Add(beacon);
            }
            //return Beacons;
            return new PagedResponse<List<BLEBeacon>>(Beacons, validFilter.PageNumber, validFilter.PageSize);
        }

        [HttpPost]
        public async Task<List<BLEBeacon>> Post([FromBody]List<RptJasonLongFormat> rpts)
        {
            List<BLEBeacon> Beacons = new List<BLEBeacon>();
            if (rpts != null && rpts.Count > 0)
            {
                foreach (RptJasonLongFormat rpt in rpts)
                {
                    HandleRptJasonLongFormat(Beacons, rpt);
                }
            }
            if (Beacons != null && Beacons.Count > 0)
            {
                await connection.OpenAsync();
                foreach (BLEBeacon tbea in Beacons)
                {
                    MySqlCommand comm = connection.CreateCommand();
                    comm.CommandText = "INSERT INTO BLEBeacon(Mac,Tag_Name,iBUUIDMajorMinor, EsNSInstanceID, Rssi, Battery, Temperature, Humidity, Vibration, ButtonPressed, Count, Time) VALUES(@Mac,@Name,@iBUUIDMajorMinor, @EsNSInstanceID, @Rssi, @Battery, @Temperature, @Humidity, @Vibration, @ButtonPressed, @Count, @Time)";
                    comm.Parameters.AddWithValue("@Mac", tbea.Mac);
                    comm.Parameters.AddWithValue("@Name", tbea.Name);
                    comm.Parameters.AddWithValue("@iBUUIDMajorMinor", tbea.iBUUIDMajorMinor);
                    comm.Parameters.AddWithValue("@EsNSInstanceID", tbea.EsNSInstanceID);
                    comm.Parameters.AddWithValue("@Rssi", tbea.Rssi);
                    comm.Parameters.AddWithValue("@Battery", tbea.Battery);
                    comm.Parameters.AddWithValue("@Temperature", tbea.Temperature);
                    comm.Parameters.AddWithValue("@Humidity", tbea.Humidity);
                    comm.Parameters.AddWithValue("@Vibration", tbea.Vibration);
                    comm.Parameters.AddWithValue("@ButtonPressed", tbea.ButtonPressed);
                    comm.Parameters.AddWithValue("@Count", tbea.Count);
                    comm.Parameters.AddWithValue("@Time", DateTime.ParseExact(tbea.Time, "yyyyMMdd HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture));
                    await comm.ExecuteNonQueryAsync();
                }
                await connection.CloseAsync();
            }
            return Beacons;
        }

        DateTime GetDateTimeFromTimeStamp(string tstr)
        {
            DateTime trev = DateTime.Now;

            if (!string.IsNullOrEmpty(tstr))
            {
                if (DateTime.TryParseExact(tstr, "yyyy-MM-ddTHH:mm:ss.fffZ", null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime tvalue) == true)
                {
                    trev = tvalue.ToLocalTime();
                }
            }

            return trev;
        }

        void HandleRptJasonLongFormat(List<BLEBeacon> Beacons, RptJasonLongFormat rpt)
        {
            BLEBeacon tBeacon = Beacons.FirstOrDefault(x => x.Mac == rpt.mac);


            if (rpt.type == "Gateway")
            {
                //Console.WriteLine("Gateway");
            }
            else if (rpt.type == "iBeacon")
            {
                //Console.WriteLine("iBeacon");
                HandleTypeiBeacon(Beacons, tBeacon, rpt);
            }
            else if (rpt.type == "S1")
            {
                //Console.WriteLine("S1");
                HandleTypeS1(Beacons, tBeacon, rpt);
            }
            else if (rpt.type == "Unknown")
            {
                //Console.WriteLine("Unknown");
                HandleTypeUnknown(Beacons, tBeacon, rpt);
            }
        }

        void HandleTypeiBeacon(List<BLEBeacon> Beacons, BLEBeacon tBeacon, RptJasonLongFormat rpt)
        {
            DateTime tDateTime = GetDateTimeFromTimeStamp(rpt.timestamp);

            if (tBeacon == null)
            {
                tBeacon = new BLEBeacon()
                {
                    Count = 0,
                    iBUUIDMajorMinor = rpt.ibeaconUuid + ":" + rpt.ibeaconMajor + ":" + rpt.ibeaconMinor,
                    Mac = rpt.mac
                };
            }
            tBeacon.Time = tDateTime.ToString("yyyMMdd HH:mm:ss");
            tBeacon.Rssi = rpt.rssi == null ? "" : rpt.rssi.ToString();
            tBeacon.Count++;
            if (tBeacon.Count == 1)
            {
                Beacons.Add(tBeacon);
            }
        }

        void HandleTypeS1(List<BLEBeacon> Beacons, BLEBeacon tBeacon, RptJasonLongFormat rpt)
        {
            DateTime tDateTime = GetDateTimeFromTimeStamp(rpt.timestamp);

            if (tBeacon == null)
            {
                tBeacon = new BLEBeacon()
                {
                    Count = 0,
                    Mac = rpt.mac
                };
            }
            tBeacon.Time = tDateTime.ToString("yyyMMdd HH:mm:ss");
            tBeacon.Rssi = rpt.rssi == null ? "" : rpt.rssi.ToString();
            tBeacon.Name = rpt.bleName;
            if (rpt.temperature != null)
            {
                tBeacon.Temperature = rpt.temperature.Value.ToString("0.#");
            }
            if (rpt.humidity != null)
            {
                tBeacon.Humidity = rpt.humidity.Value.ToString("0.##");
            }
            tBeacon.Count++;
            if (tBeacon.Count == 1)
            {
                Beacons.Add(tBeacon);
            }
        }

        void HandleUnkown_Eddystone_UID(List<BLEBeacon> Beacons, BLEBeacon tBeacon, RptJasonLongFormat rpt)
        {
            if (tBeacon == null)
            {
                tBeacon = new BLEBeacon() { Count = 0, Mac = rpt.mac };
            }
            tBeacon.EsNSInstanceID = rpt.rawData.Substring(26, 20) + "-" + rpt.rawData.Substring(46, 12);
            DateTime tDateTime = GetDateTimeFromTimeStamp(rpt.timestamp);
            tBeacon.Time = tDateTime.ToString("yyyMMdd HH:mm:ss");
            tBeacon.Rssi = rpt.rssi == null ? "" : rpt.rssi.ToString();
            tBeacon.Count++;
            if (tBeacon.Count == 1)
            {
                Beacons.Add(tBeacon);
            }
        }

        void HandleUnkown_Eddystone_URL(List<BLEBeacon> Beacons, BLEBeacon tBeacon, RptJasonLongFormat rpt) // indicates that double button tap, need to set trigger and url to doubletap in beacons
        {
            string turl = rpt.rawData.Substring(28, 18);

            if (turl == "646F75626C65746170") // ASCII of "doubletap"
            {
                if (tBeacon == null)
                {
                    tBeacon = new BLEBeacon() { Count = 0, Mac = rpt.mac };
                }

                tBeacon.ButtonPressed = "Double tapped";

                DateTime tDateTime = GetDateTimeFromTimeStamp(rpt.timestamp);
                tBeacon.Time = tDateTime.ToString("yyyMMdd HH:mm:ss");
                tBeacon.Rssi = rpt.rssi == null ? "" : rpt.rssi.ToString();
                tBeacon.Count++;
                if (tBeacon.Count == 1)
                {
                    Beacons.Add(tBeacon);
                }
            }
        }

        void HandleUnkown_PressButton_Once(List<BLEBeacon> Beacons, BLEBeacon tBeacon, RptJasonLongFormat rpt) // indicates that double button tap
        {
            if (tBeacon == null)
            {
                tBeacon = new BLEBeacon() { Count = 0, Mac = rpt.mac };
            }

            tBeacon.ButtonPressed = "Once tapped";

            DateTime tDateTime = GetDateTimeFromTimeStamp(rpt.timestamp);
            tBeacon.Time = tDateTime.ToString("yyyMMdd HH:mm:ss");
            tBeacon.Rssi = rpt.rssi == null ? "" : rpt.rssi.ToString();
            tBeacon.Count++;
            if (tBeacon.Count == 1)
            {
                Beacons.Add(tBeacon);
            }
        }

        public string HEX2ASCII(string hex)
        {
            string res = string.Empty;

            for (int a = 0; a < hex.Length; a = a + 2)
            {
                string Char2Convert = hex.Substring(a, 2);
                int n = Convert.ToInt32(Char2Convert, 16);
                char c = (char)n;
                res += c.ToString();
            }
            return res;
        }
        void HandleUnkown_DeviceInfo(List<BLEBeacon> Beacons, BLEBeacon tBeacon, RptJasonLongFormat rpt)
        {
            string tDLlenStr = rpt.rawData.Substring(14, 2);
            if (int.TryParse(tDLlenStr, NumberStyles.HexNumber, null, out int tDLLen) == true)
            {
                string trawstr = rpt.rawData.Substring(16);
                if (trawstr != null && trawstr.StartsWith("16E1FFA108") == true)
                {
                    string tBatteryStr = trawstr.Substring(10, 2);
                    if (int.TryParse(tBatteryStr, NumberStyles.HexNumber, null, out int tBattery) == true)
                    {
                        if (tBeacon == null)
                        {
                            tBeacon = new BLEBeacon() { Count = 0, Mac = rpt.mac };
                        }
                        DateTime tDateTime = GetDateTimeFromTimeStamp(rpt.timestamp);
                        tBeacon.Time = tDateTime.ToString("yyyMMdd HH:mm:ss");
                        tBeacon.Battery = tBattery + "%";
                        tBeacon.Count++;
                        string tblename = trawstr.Substring(24, tDLLen * 2 - 24);
                        if (tblename != null)
                        {
                            tBeacon.Name = HEX2ASCII(tblename);
                        }
                        if (tBeacon.Count == 1)
                        {
                            Beacons.Add(tBeacon);
                        }
                    }
                }
            }
        }

        void HandleUnkown_ACC_Axis(List<BLEBeacon> Beacons, BLEBeacon tBeacon, RptJasonLongFormat rpt)
        {
            string tBatteryStr = rpt.rawData.Substring(26, 2);
            if (int.TryParse(tBatteryStr, NumberStyles.HexNumber, null, out int tBattery) == true)
            {
                if (tBeacon == null)
                {
                    tBeacon = new BLEBeacon() { Count = 0, Mac = rpt.mac };
                }

                DateTime tDateTime = GetDateTimeFromTimeStamp(rpt.timestamp);
                tBeacon.Time = tDateTime.ToString("yyyMMdd HH:mm:ss");

                tBeacon.Battery = tBattery + "%";
                tBeacon.Count++;
                tBeacon.Vibration = "Yes";

                if (tBeacon.Count == 1)
                {
                    Beacons.Add(tBeacon);
                }
            }
        }

        void HandleTypeUnknown(List<BLEBeacon> Beacons, BLEBeacon tBeacon, RptJasonLongFormat rpt)
        {
            if (!string.IsNullOrEmpty(rpt.rawData))
            {
                try
                {
                    if (rpt.rawData == "0201060303AAFE11079ECADC240EE5A9E093F304820100287F")
                    {
                        //Console.WriteLine("mac: " + rpt.mac + ", rawData: " + rpt.rawData);
                        HandleUnkown_PressButton_Once(Beacons, tBeacon, rpt);
                    }
                    else if (rpt.rawData.StartsWith("0201060303AAFE1516AAFE00")) // Eddystone UID
                    {
                        //Console.WriteLine("mac: " + rpt.mac + ", rawData: " + rpt.rawData);
                        HandleUnkown_Eddystone_UID(Beacons, tBeacon, rpt);
                    }
                    else if (rpt.rawData.StartsWith("0201060303AAFE1016AAFE10")) // Eddystone URL is used to indicate double button click
                    {
                        //Console.WriteLine("mac: " + rpt.mac + ", rawData: " + rpt.rawData);
                        HandleUnkown_Eddystone_URL(Beacons, tBeacon, rpt);
                    }
                    else if (rpt.rawData.StartsWith("0201060303E1FF1216E1FFA103")) // ACC-Axis
                    {
                        //Console.WriteLine("mac: " + rpt.mac + ", rawData: " + rpt.rawData);
                        HandleUnkown_ACC_Axis(Beacons, tBeacon, rpt);
                    }
                    else if (rpt.rawData.StartsWith("0201060303E1FF")) // Device Info
                    {
                        //Console.WriteLine("mac: " + rpt.mac + ", rawData: " + rpt.rawData);
                        HandleUnkown_DeviceInfo(Beacons, tBeacon, rpt);
                    }

                }
                catch (Exception ex)
                {

                }
            }
        }

        public class RptJasonLongFormat
        {
            public string timestamp { get; set; }
            public string type { get; set; }
            public string mac { get; set; }
            public int? rssi { get; set; }

            public int? gatewayFree { get; set; }
            public float? gatewayLoad { get; set; }

            public string bleName { get; set; }
            public string ibeaconUuid { get; set; }
            public int? ibeaconMajor { get; set; }
            public int? ibeaconMinor { get; set; }
            public int? ibeaconTxPower { get; set; }
            public int? battery { get; set; }

            public float? temperature { get; set; }
            public float? humidity { get; set; }

            public string rawData { get; set; }
        }
    }
}
