# Introduction
Bought two of the HanmaTek HM610P switching power supply to replace my discommissioned decades old one,
happy with it features in general. Especially it came with software that can control it through USB interface on Windows, but 
the software does not do what I wanted.  

I want to control it voltage, current, over current protect, able to turn on/off with varies of presets for
specific project or boards.  That software just does not do a good job for me.  

With documents on its CD I found that it use MODBUS protocol on serial (FTDI-USB), and some setting found
in the applicaiton setting XML files I was able to found some userful register address that is useful to control it.  

That encourage me to develop this program to control the power supply, I can now run a batch file to set varies of
power parameters for my own project.  I also hope other owner of this power supply may found it userful, or get
source code enhance it or just change it to meet your specific need.

# Program
It is a simple program writtern in C#


