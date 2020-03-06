using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using HanmatekPSLib;
using CommandLine;
using System.Configuration;
using CommandLine.Text;

namespace hmps_cmd
{
    public enum OnOffSwitch
    {
        __NotDefined__,
        On,
        Off
    };

    class Options
    {
        [Value(0, Default=null, MetaName = "COM", 
            HelpText ="COM port of the Power supply")]
        public String comPort { get; set; }

        [Option("ListPorts", Default = false, SetName = "NoJob",    // 
            HelpText = "List COM ports in the system, need to put dummp COM port to have it works")]
        public bool ListCOMPorts { get; set; }

        [Option("ShowPSU", Default = false, 
            HelpText = "Show PSU information")]
        public bool ShowPSU { get; set; }



        [Option("Power", Default = OnOffSwitch.__NotDefined__, // Min = (int) OnOffSwitch.On, Max = (int) OnOffSwitch.Off,
            HelpText = "On/Off - turn power Power on or off")]
        public OnOffSwitch SetPowerSwitch { get; set; }

        [Option('V', "SetV", Default = null,
            HelpText = "Set Power Voltage - range 0.0-32.0")]
        public string setVoltageStr { get; set; }
        [Option('A', "SetA", Default = null,
            HelpText = "Set Current limit - range 0.0-10.100")]
        public string setCurrentLimitStr { get; set; }

        [Option("SetOVP", Default = null,
            HelpText = "Set Over Voltage Protection Limit - range 0.0-33.0, need trun it on/off with button")]
        public string setOVPStr { get; set; }

        [Option("SetOCP", Default = null,
            HelpText = "Set Over Current Protection - range 0.0-10.500, need trun it on/off with button")]
        public string setOCPStr { get; set; }

        [Option("Buzzer", Default = OnOffSwitch.__NotDefined__,
            HelpText = "On/Off - Turn Buzzer On or Off")]
        public OnOffSwitch SetBuzzer { get; set; }

        [Option('s', "Silent", Default = false,
            HelpText = "Disable normal messages")]
        public bool Silent { get; set; }

    }

    class Program
    {
        static public Options options=null;
        static public bool Error = false;
        static HanmatekPS psu = null;
        static public bool HelpShown = false;
        struct testSt
        {
            public ushort Exp;
            public string StrVal;
            public int dec;
            public float min;
            public float max;
        };
        public static void TestCode()
        {

           testSt[] testArry = new testSt[] {
                    new testSt{Exp=3200,StrVal="32.0",dec=2, min=0.0F,max=32.0F },
                    new testSt{Exp=1045,StrVal="10.450",dec=2, min=0.0F,max=32.0F },
                    new testSt{Exp=10450,StrVal="10.450",dec=3, min=0.0F,max=32.0F },
                    new testSt{Exp=10,StrVal="0.10",dec=2, min=0.0F,max=32.0F },
                    new testSt{Exp=234,StrVal="0.234",dec=3, min=0.0F,max=32.0F },
                    new testSt{Exp=1234,StrVal="1.234",dec=3, min=0.0F,max=32.0F },
                    new testSt{Exp=20,StrVal="0.2",dec=2, min=0.0F,max=32.0F },
                    new testSt{Exp=10000,StrVal="10",dec=3, min=0.0F,max=10.50F },
                    new testSt{Exp=1,StrVal=".001",dec=3, min=0.0F,max=32.0F },
                    new testSt{Exp=1,StrVal="0.011",dec=2, min=0.0F,max=32.0F }
            };
            ushort Val;
            for (int i = 0; i < testArry.Length; i++)
            {
                Val = ConvCheckValue(testArry[i].StrVal, testArry[i].dec, testArry[i].min, testArry[i].max);
                if (Val != testArry[i].Exp)
                {
                    Console.WriteLine("Fail to match[{0}] {1} - {2}:",i, Val, testArry[i].StrVal, testArry[i].dec);
                }
            }
        }
        static void Main(string[] args)
        {
            // Setup custom parser to set case sensitive off 
            var parser = new Parser(settings =>
            {
                settings.CaseSensitive = false;
				settings.CaseInsensitiveEnumValues = false;
                settings.HelpWriter = Console.Error;  // reroute back to Console.Error for messages
            });

            //            var result = Parser.Default.ParseArguments<Options>(args)
            var result = parser.ParseArguments<Options>(args)
                .WithParsed(optionsChecker)  // was .WithParsed(opt => options = opt) to just set options to parsed
                .WithNotParsed(HandleParseError);

            if (options == null || Error == true)
            {
                if (!HelpShown)
                {
                    Console.WriteLine("HM-PS Control CMD by Mcken Mak 2020\nUse on HanmaTEK power Supply HM310P or lower model.");
                    Console.WriteLine("\n\n" + HelpText.AutoBuild(result, _ => _, _ => _) + "\nERROR - See above");
                    HelpShown = true;
                }
                return;
            }
            if (!options.Silent) Console.WriteLine("HM-PS Control CMD by Mcken Mak 2020\nUse on HanmaTEK power Supply HM310P or lower model.");


            psu = new HanmatekPS();
            try
            {
                // Options that do not operate PSU or will terminate
                if (options.ListCOMPorts)
                {
                    Console.WriteLine("\nList COM ports in the system:");
                    string[] ports = SerialPort.GetPortNames();
                    Array.Sort(ports, StringComparer.InvariantCulture);
                    foreach (string p1 in ports)
                        Console.Write("{0} ", p1);
                    Console.WriteLine("\n\nExit skips any PSU task.");
                    return;
                }

                // Start PSU operations
                if (!psu.Open(options.comPort))
                {
                    Console.WriteLine("Open COM port [{1}] Error: {0}", psu.getModbugStatus(), options.comPort);
                    return;
                }

                if (options.ShowPSU) ShowPSUInfo();
                // Set Basics
                if (options.setVoltageStr != null && (options.setCurrentLimitStr != null))  // set both for faster performance
                {
                    ushort Volt = ConvCheckValue(options.setVoltageStr, 2, 0.0F, 32.0F, "SetVol");
                    ushort Curr = ConvCheckValue(options.setCurrentLimitStr, 3, 0.0F, 10.10F, "SetCur");
                    if (!options.Silent) Console.WriteLine("Set both Vol[{0}/{1}]/Cur[{2}/{3}].",options.setVoltageStr,Volt, options.setCurrentLimitStr,Curr);
                    psu.setVoltageAndCurrent(Volt, Curr);
                }
                else
                {
                    if (options.setVoltageStr != null)
                    {
                        ushort val = ConvCheckValue(options.setVoltageStr, 2, 0.0F, 32.0F, "SetVol");

                        if (!options.Silent) Console.WriteLine("Setting Voltage to [{0}]/{1}", options.setVoltageStr, val);
                        psu.setVoltage(val);
                    }
                    if (options.setCurrentLimitStr != null)
                    {
                        ushort val = ConvCheckValue(options.setCurrentLimitStr, 3, 0.0F, 10.10F, "SetCur");

                        if (!options.Silent) Console.WriteLine("Setting Current limit to [{0}]/{1}", options.setCurrentLimitStr, val);
                        psu.setCurrent(val);
                    }
                }
                // Set Protections
                if (options.setOVPStr != null)
                {
                    ushort val = ConvCheckValue(options.setOVPStr, 2, 0.0F, 33.0F, "setOVP");

                    if (!options.Silent) Console.WriteLine("Setting Over Voltage Protection to [{0}]/{1}, need to set manually", options.setOVPStr, val);
                    psu.setOVP(val);
                }
                if (options.setOCPStr != null)
                {
                    ushort val = ConvCheckValue(options.setOCPStr, 3, 0.0F, 10.50F, "setOCP");

                    if (!options.Silent) Console.WriteLine("Setting Over Current Protection to [{0}]/{1}, need to set manually", options.setOCPStr, val);
                    psu.setOCP(val);
                }

                // Set Switches
                if (options.SetBuzzer != OnOffSwitch.__NotDefined__)
                {
                    if (!options.Silent) Console.WriteLine("Buzzer set to [{0}]", options.SetBuzzer);
                    psu.setBuzzer(options.SetBuzzer == OnOffSwitch.On ? true : false);
                }


                if (options.SetPowerSwitch != OnOffSwitch.__NotDefined__)
                {
                    if (!options.Silent) Console.WriteLine("Power set to [{0}]", options.SetPowerSwitch);
                    psu.setPower(options.SetPowerSwitch == OnOffSwitch.On ? true : false);
                }
                psu.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }

        // Return 
        static ushort ConvCheckValue(string value, int dec, float min, float max, string Func=null)
        {
            try
            {
                float fvalue = float.Parse(value);
                if (fvalue > max) throw new ArgumentOutOfRangeException(String.Format("{0}:[{1}] Greater than max - {2:f2}", Func, value,max));
                if (fvalue < min) throw new ArgumentOutOfRangeException(String.Format("{0}:[{1}] Small than min - {2:f2}", Func, value, min));
                float multipler = (float)Math.Pow(10.0F, Convert.ToSingle(dec));
                fvalue = fvalue * multipler;
                return Convert.ToUInt16(fvalue);
            }
            catch (Exception ex)
            {
                throw;
            }
            //return 0;
        }

        private static void optionsChecker(Options opts)
        {
            options = opts;

            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports, StringComparer.InvariantCulture);

            if (options.comPort == null)
            {
                String sVal = ConfigurationManager.AppSettings.Get("Default");
                if (sVal != null)
                {
                    options.comPort = sVal;
                    if (!options.Silent) Console.WriteLine("Useing default port [{0}]", sVal);
                }
                else
                {
                    Console.WriteLine("ERROR: Needed to put COM port as first parameter");
                    Error = true;
                    return;
                }
            }
            options.comPort = options.comPort.ToUpper();

            if (!ports.Contains(options.comPort))
            {
                // Check its a alias in config
                String sVal = ConfigurationManager.AppSettings.Get(options.comPort);
                if (sVal != null)
                {
                    if (!options.Silent) Console.WriteLine("Alias [{0}] = port [{1}] from hmps-cmd.exe.config", options.comPort, sVal);
                    options.comPort = sVal.ToUpper();
                }
                if (!ports.Contains(options.comPort))  // test one more time if its valid
                {
                    Console.Write("COM port [{0}] is not valid, available ports are:\n    ", options.comPort);
                    foreach (string p1 in ports)
                    {
                        Console.Write("{0} ", p1);
                    }
                    Console.WriteLine("");
                    Error = true;
                    return;
                }
            }
            

            return;
        }


        private static void HandleParseError(IEnumerable<Error> errs)
        {

            if (errs.IsVersion())
            {
                Console.WriteLine("Version Request");
                return;
            }
            HelpShown = true;

            if (errs.IsHelp())
            {
                Console.WriteLine("Help Request");
                Console.WriteLine("Example: > hmps-cmd COM3 --setVol 5.00 --setCur 1.3 --Power On");
                Console.WriteLine("\nNOTE: File hmps_cmd.exe.config can Set default port set and alias for ports.");
                return;
            }
            Console.WriteLine("Commandline Parser Failed");
        }

        public static void ShowPSUInfo()
        {
            psu.showPSUInfo();

        }
    }
}
