# Introduction
Bought two of the HanmaTek HM610P switching power supply to replace my discommissioned decades old one,
happy with it features in general. Especially it came with software that can control it through USB interface on Windows, but 
the software does not do what I wanted.  

Later found out this HanmaTek power supply actually is from eTOMMENS, a China power supply manufactor, wher it model is eTM-3010P, now some of the information found from the application's setting XML file start making senses.  http://www.etommens.com/products_content-2023956.html.  

What I want are:  ontrol it voltage, current, over current protect, able to turn on/off with varies of presets for
specific project or boards.  That software just does not do a good job for me.  

With documents on its CD I found that it use MODBUS protocol on serial (FTDI-USB), and some setting found
in the applicaiton setting XML files I was able to found some userful register address that is useful to control it.  

That encourage me to develop this program to control the power supply, I can now run a batch file to set varies of
power parameters for my own project.  I also hope other owner of this power supply may found it userful, or get
source code enhance it or just change it to meet your specific need.

Source code it in https://github.com/mckenm/HanmaTekPSUCmd  
  
You can download the release version of code and binary from https://github.com/mckenm/HanmaTekPSUCmd/releases


Hope you will found some use of it!


# Program
This simple program is writtern in C#, and depends on CommandLine and System.IO.Port.

To use it just unzip the compiled binary and run the hmps-cmd.

Program syntax is:

hmps-cmd Port/Alias [options]

You can put multiple options to it command line, if same options only will use the first one.  Say if you like to set V=5, A=1.1, OCP=1, then you can do the following:

>hmps-cmd COM5 --SetV 5 --SetA 1.1 --SetOCP 1

You can set default port by edit the file hmps-cmd.exe.config:
```XML
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2" />
    </startup>
  <appSettings>
    <add key="Default" value="COM6" />
    <add key="PSU1" value="COM6" />
    <add key="PSU2" value="COM7" />
  </appSettings>
</configuration>
```
Just update the Default's value from COM6 to match your enviornment. To use the default, just do the following:  
> hmps-cmd --SetV 5 --SetA 1.1 --SetOCP 1


Or if you want to set alias for your port, the "PSU1" and "PSU2" you can change that to something you like and matching the port on your system.
You can add additional alias (key) to this file, just follow the same format.

To use alisa with the above example to PSU2 that set to COM7, just do the following:  
>hmps-cmd PSU2 --SetV 5 --SetA 1.1 --SetOCP 1



```
> hmps-cmd.exe --help
hmps-cmd 0.1.0.0
Mcken Mak - Copyright 2020

  --ListPorts     (Default: false) List COM ports in the system, need to put dummp COM port to have it works
  --ShowPSU       (Default: false) Show PSU information
  --Power         (Default: __NotDefined__) On/Off - turn power Power on or off
  -V, --SetV      Set Power Voltage - range 0.0-32.0
  -A, --SetA      Set Current limit - range 0.0-10.100
  --SetOVP        Set Over Voltage Protection Limit - range 0.0-33.0, need trun it on/off with button
  --SetOCP        Set Over Current Protection - range 0.0-10.500, need trun it on/off with button
  --Buzzer        (Default: __NotDefined__) On/Off - Turn Buzzer On or Off
  -s, --Silent    (Default: false) Disable normal messages
  --help          Display this help screen.
  --version       Display version information.
  COM (pos. 0)    COM port of the Power supply

Help Request
Example: > hmps-cmd COM3 --setVol 5.00 --setCur 1.3 --Power On

NOTE: File hmps_cmd.exe.config can Set default port set and alias for ports.
```
