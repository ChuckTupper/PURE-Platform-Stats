PURE-Platform-Stats
===================

Hourly log of BF4 platform stats

Program will pull platform statistics hourly from http://api.bf4stats.com/api/onlinePlayers?output=lines.

Information gets put into a MySQL database.

Uses .net 3.5



TO DO
=====

Add Timers to run hourly
Finish exception handling
--in the event of a failure, retry in x minutes
