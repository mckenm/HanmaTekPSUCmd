using System;
using System.IO.Ports;

namespace HanmatekPSLib
{
    public class HanmatekPS
    {
        private modbus mb;
        private byte DeviceAddr = 1;

        const ushort PS_PowerSwitch = 0x01;     // R/W  0/1  Power output/stop setting
        const ushort PS_ProtectStat = 0x02;     // R  - Bit mask (OCP 0x02/OVP 0x01) Ptotect status -SCP:OTP:OPP:OCP:OVP
                                                //      OVP：Over voltage protection OCP：Over current protection OPP：Over power protection
                                                //      OTP：Over tempreture protection SCP：short-circuit protection
        const ushort PS_Model = 0x03;           // R  - 3010 (HM310P)
        const ushort PS_ClassDetial = 0x04;     // R  - 0x4b58 (19280)  
        const ushort PS_Decimals = 0x0005;      // R  - 0x233
                                                //      Note 2: Decimal point digit capacity information as follow:
                                                //      voltage current power decimal point digit capacity
                                                //          Dat=ShowPN;//((2<<8)|(3<<4)|(3<<0));//0.00V 0.000A 0.000W
                                                //      For example when read:0x0233 (563)
                                                //          mean that voltage 2 decimal,current 3 decimal,power 3 decimal.
        const ushort PS_Voltage = 0x0010;       // R 2Dec Voltage display value
        const ushort PS_Current = 0x0011;       // R 3Dec Current display value 
        const ushort PS_PowerH = 0x0012;         // R 3Dec Power display value 0012H(high 16 bit)/ 0013H( low 16 bit )
        const ushort PS_PowerL= 0x0013;         // R 3Dec Power display value 0012H(high 16 bit)/ 0013H( low 16 bit )
        const ushort PS_PowerCal = 0x0014;

        const ushort PS_ProtectVol = 0x0020;    // RW 2Dec OVP Set over volate protect value
        const ushort PS_ProtectCur = 0x0021;    // RW 2Dec OCP Set over current protect value
        const ushort PS_ProtectPow = 0x0022;    // RW 2Dec OPP Set over power protect value 0022H(high 16 bit)，023H(low 16 bit)

        const ushort PS_SetVoltage = 0x0030;        // RW 2Dec Set voltage
        const ushort PS_SetCurrent = 0x0031;        // RW 3Dec Set current 
        const ushort PS_SetTimeSpan = 0x0032;   // RW ????


        const ushort PS_PowerStat = 0x8801;
        const ushort PS_defaultShow = 0x8802;
        const ushort PS_SCP = 0x8803;
        const ushort PS_Buzzer = 0x8804;
        const ushort PS_Device = 0x9999;        // R/W Set communication address - SlaveID = 1 default
        const ushort PS_SDTime = 0xCCCC;
        const ushort PS_UL = 0xC110;            // 11d / xC111=1
        const ushort PS_UH = 0xC11E;            // 3200d / xC11F=1, 
        const ushort PS_IL = 0xC120;            // 21 / xC121=1
        const ushort PS_IH = 0xC12E;            // 10100/ xC12F=1

        const ushort PSM_Voltage = 0x1000;
        const ushort PSM_Current = 0x1001;
        const ushort PSM_TimeSpan = 0x1002;
        const ushort PSM_Enable = 0x1003;
        const ushort PSM_NextOffset = 0x10;


        public HanmatekPS() =>   mb = new modbus(1);  // Default DeviceID = 1
        public HanmatekPS(byte DevID) => DeviceAddr = DevID;


        public bool Open(string port)
        {
            return mb.Open(port, 9600, 8, Parity.None, StopBits.One); // only support 9600B:8N1
        }
        public bool Close()
        {
            return mb.Close();
        }
        public string getModbugStatus() { return mb.modbusStatus; }

        public bool setPower(bool state)
        {
            return mb.SendFc6(DeviceAddr, PS_PowerSwitch, (short) (state == true ? 1 : 0)); // Turn power on 
        }
        public bool setBuzzer(bool state)
        {
            return mb.SendFc6(DeviceAddr, PS_Buzzer, (short)(state == true ? 1 : 0)); 
        }

        public bool setVoltage(ushort Voltage)
        {
            return mb.SendFc6(DeviceAddr, PS_SetVoltage, (short)Voltage); 
        }
        public bool setCurrent(ushort Current)
        {
            return mb.SendFc6(DeviceAddr, PS_SetCurrent, (short)Current);
        }

        public bool setVoltageAndCurrent(ushort Voltage, ushort Current)
        {
            short[] Values = new short[2];
            Values[0] = (short)Voltage; Values[1] = (short) Current;
            return mb.SendFc16(DeviceAddr, PS_SetVoltage, 2, Values);
        }

        public bool setOVP(ushort Voltage)
        {
            return mb.SendFc6(DeviceAddr, PS_ProtectVol, (short)Voltage);
        }
        public bool setOCP(ushort Current)
        {
            return mb.SendFc6(DeviceAddr, PS_ProtectCur, (short)Current);
        }

        public void printValues(ushort StartAddr, ushort num, short[] values)
        {
            for (int i = 0; i < num; i++)
            {
                Console.WriteLine("[0x{0:X4}][{0:D4}]={1:D5}/0x{1:X4}", (StartAddr + i), values[i]);
            }
        }

        public void SendAndPrint(ushort StartAddr, ushort num)
        {
            short[] values = new short[num];
            try
            {
                mb.SendFc3(DeviceAddr, StartAddr, num, ref values);
            }
            catch (Exception err)
            {
                Console.WriteLine("Error in modbus read: " + err.Message);
                return;
            }
            printValues(StartAddr, num, values);

        }

        public void showPSUInfo()
        {
            Console.WriteLine("Showing Power Supply System Info");
            short[] values = new short[4];
            try
            {
                mb.SendFc3(DeviceAddr, PS_PowerSwitch, 4, ref values);
                Console.WriteLine("Model={0},  Power={1}\n-----------------------------------",values[2],values[0] ==0 ? "Off": "On");
                short pv = values[1];

                mb.SendFc3(DeviceAddr, PS_SetVoltage, 2, ref values);
                float sVolt = Convert.ToSingle(values[0]) / 100.0F, sCurrent = Convert.ToSingle(values[1]) / 1000.0F;
                mb.SendFc3(DeviceAddr, PS_Voltage, 4, ref values);
                float cVolt = Convert.ToSingle(values[0]) / 100.0F,
                    cCurrent = Convert.ToSingle(values[1]) / 1000.0F;
                // just test UInt32 v = ((UInt32)(1) << 16);
                UInt32 cIntWatt = (UInt32 )((ushort)values[3]) + ((UInt32)(values[2]) << 16);  // Lo-Hi concat
                double cWatt = Convert.ToDouble(cIntWatt) / 1000.0;

                mb.SendFc3(DeviceAddr, PS_ProtectVol, 2, ref values);
                float OVP= Convert.ToSingle(values[0]) / 100.0F,
                      OCP= Convert.ToSingle(values[1]) / 1000.0F;
                ConsoleColor orgCol = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Cur   V={0:00.00},  A={1:00.000}, W={2:00.000}", cVolt, cCurrent, cWatt);
                Console.WriteLine("Set   V={0:00.00},  A={1:00.000}", sVolt, sCurrent);
                Console.WriteLine("Set OVP={0:00.00},OCP={1:00.000}", OVP, OCP);
                Console.ForegroundColor = orgCol;
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("SCP({0}) OTP({1}) OPP({2}) OCP({3}) OVP({4})",
                                    (pv & 0x10) == 0 ? 0 : 1,
                                    (pv & 0x08) == 0 ? 0 : 1,
                                    (pv & 0x04) == 0 ? 0 : 1,
                                    (pv & 0x02) == 0 ? 0 : 1,
                                    (pv & 0x01) == 0 ? 0 : 1);

            }
            catch (Exception err)
            {
                Console.WriteLine("Error in modbus read: " + err.Message);
                return;
            }

        }
        public void  getTest()
        {
            //mb.SendFc6(DeviceAddr, 1, 1) ;  // Turn power on 
            Console.WriteLine("System");
            SendAndPrint(PS_PowerSwitch, 0x30);
/*
            Console.WriteLine("Current Pos");
            SendAndPrint(PS_Voltage, 5);
            Console.WriteLine("Protection");
            SendAndPrint(PS_ProtectVol, 3);
            Console.WriteLine("Internal");
            SendAndPrint(PS_PowerStat,4);
*/
        }

    }
}
