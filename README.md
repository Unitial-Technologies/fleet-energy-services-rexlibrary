# ReXlibrary

BinRXD - Used for decoding Influx Technology .rxd files and converting .xml configuration files to .rxc (ReXgen configuration) files. Supports converting RXD files to CSV, InfluxDB CSV, MDF, Matlab, BLF, ASCII, and TRC format.

DbcParser – Used for parsing DBC files.

InfluxShared – Various units used by the different packages.

Matlab File – Used for export to Matlab files.

MDF4xx – Used for export to MDF Files.

RXD/RXDDemo – Example that converts configuration xml file to rxc file used for the ReXgen device. It uses a ReXConfig.xsd file to validate the xml. It can also open rxc files.

Demo/AWS\_Lambda – AWS Lambda example that takes an xml file after upload to S3 and converts it to rxc. It also converts rxd files to csv and can be changed to export to CSV InfluxDB format, Matlab, BLF, etc.
