This cross platform (Windows, Linux, MacOS) project provides an utility that enables the sending and receiving of text
SMS messages through a Huawei USB dongle, without the need to change modes on the dongle to modem mode, or issue AT
commands.

It is split into three major parts:
* A library for interfacing with the Huawei USB dongle's API for accessing SMS messages.
* A long-running service for monitoring, receiving and processing new text messages.
* A command line utility for sending text messages through the dongle.

Received messages are forwarded to an email address, or added to a MQTT topic.

It has been develped exclusively against:
* E3372-325

But is expected to work with other Huawei modems using a similar API (please report back on compatibility).

The focus of this project is SMS.  Please submit PRs for changes.

# Downloading and Installing
The latest release version can be downloaded from [GitHub](https://github.com/Dash/Ox.Huawei.Sms/releases).

For Windows, download the *.win64.zip, for Linux either download the RPM and install, or *.linux64.tar.gz.  Follow
the configuration guide below to setup for your device.  There is no Windows installer at this time.  There is no
Mac-targetted build, instead you will either have to build (see instructions below), or download the portable version
and execute with the dotnet runtime.

# SMS Monitor
The Ox.Huawei.Sms.Monitor project defines a background service that will connect to the HTTP endpoint
for the dongle and monitor for new SMS messages.  New SMS messages are then dispatched to a configured
endpoint.

## Building from source
The project is designed to target the latest LTS .NET runtime, at time of writing .NET 10.0.

Ensure you have the .NET SDK installed, either from your package manager, from the
[download page](https://dotnet.microsoft.com/en-us/download), or from Visual Studio.

From the directory of the project you want to build, if building from command line, run:
	dotnet publish -c Release -p:DebugSymbols=false -p:PublishProfile=***profile***

Where profile is either:
* LinuxSelfContained
* WindowsSelfContained

This will output the binaries to the respective bin/Release/net10.0/***platform***/publish.  The two executable projects
are:
* Ox.Huawei.Sms.Monitor - the SMS monitoring service (smsmonitor)
* Ox.Huawei.Sms.SendCLI - command line utility to send SMS (sendsms)

Other directories are libraries that are pulled in as dependencies for the executables.

You can of course, do a simple "dotnet build" to get a version with all the debug symbols and what not, and this is 
perfectly fine to use.

## Configuration
Standard .NET Application Settings are used, appsettings.json is provided and values can 
be changed, or alternatively, environment variables can be used.

To save your configuration being overwritten by new versions, you can use
appsettings.Production.json, which will override any example/default settings in 
appsettings.json.

For Linux, you can move the appsettings.json file to /etc/smsmonitor.json.  Settings are merged with other sources,
including environment variables.

### Device
The device base address is the HTTP endpoint for the device.  This is typically on 192.168.8.1, and the
full path of the API needs to be provided, with the trailing slash.
```json
"Device": {
	"BaseAddress": "http://192.168.8.1/api/"
}
```

### Polling
There are two device monitors defined, one for SMS and one for the device status.  The polling interval is defined in
seconds. The SMS monitor will check for new messages every 5 seconds, and the device status every 60 seconds.  If the an
error is encountered with the device monitor, it will attempt to restart the device and retry.

If error conditions persist for a second polling period, the application will exit after dispatching an error
notification.  This applies to the SMS Monitor too, which will most likely to be the process to identify fatal errors
first.  The Device Monitor will mostly fail on loss of signal.

```json
"Monitors": {
	"SmsMonitor": {
		"PollInterval": 5,
		"DeleteAfterDispatch": false
	},
	"DeviceMonitor": {
		"PollInterval": 60
	}
}
```

## Receive Dispatchers

Receive dispatchers take a received SMS and dispatches it somwhere.  They implement the
IDispatcher interface. To add new ones in, extend the interface and the relevant
configuration and initalisation code into the Ox.Huawei.Sms.Monitor.Program startup class.

### SMTP (Email)
To forward SMS' to email, ensure the SMTP details are defined in the appsettings.json file:

```json
"Dispatchers": {
	"SmtpDispatcher": {
		"Host": "email.server.hostname",
		"ToAddress": "email@address.to.send",
		"FromAddress": "sms@localhost",
		"Port": 25
	}
}
```
*Mandatory fields*
* Host
* ToAddress

### MQTT (Messaging)
To forward SMS' to an MQTT broker, ensure the MQTT details are defined in the appsettings.json file:

```json
"Dispatchers": {
	"MqttDispatcher": {
		"Host": "localhost",
		"Port": 1883,
		"TopicName": "sms/send",
		"Username": "",
		"Password": "",
		"UseTls": false,
		"ReconnectDelaySeconds": 5,
		"MqttVersion": 3,
		"TimeoutSeconds": 30
	}
}
```

Mandatory fields:
* Host
* Port
* TopicName
* Username
* Password

When an SMS is received, a JSON message is published to the configured topic.
The message format is as follows:

```json
{
		"MessageType": "SmsReceived",
		"Timestamp": "2010-12-31T23:59:59",
		"Sms": {
				"From": "\u002B441234567890",
				"Date": "010-12-31T23:59:59",
				"Content": "Hello, world!"
		}
}
```

## Running
At the simplest level, simply run the monitor application.

    ./smsmonitor
Or

	smsmonitor.exe

You may wish to use a program like "screen" on Linux to allow it to run in a detached
state.  Or run it as a service.  An example systemd service file is provided in this
repository.

# Sending SMS

## Command Line Interface
The Ox.Huawei.Sms.SendCLI program acts independently of the monitor application and does
not require the monitor to be running in order to send.

Use "sendsms" (or sendsms.exe) to run the program.

Arguments:

| Switch | Description | Required | Example |
|--|--|--|--|
| -t | List of TO addresses, comma separated | Yes | -t +441234567890,+441234567891 |
| -b | Override the device base address | No | -b http://192.168.8.20/api/ |

The message body is captured from the standard input, which allows you either to type
your message in, or to pipe it in from another command.  A blank line will terminate
interactive input.

	./sendsms -t +441234567890
or

	./sendsms -t +441234567890 < message.txt

## MQTT
To send an sms via MQTT, the monitor application needs to be running.  Configure
the MQTT services and publish a message to the configured topic.

```json
"Monitors": {
	"MqttMonitor": {
		"Host": "localhost",
		"Port": 1883,
		"TopicName": "sms/send",
		"Username": "",
		"Password": "",
		"UseTls": false,
		"ReconnectDelaySeconds": 5,
		"MqttVersion": 3,
		"TimeoutSeconds": 30
	}
}
```

Mandatory fields:
* Host
* Port
* TopicName
* Username
* Password

Publish to the configured topic a JSON message in the following format:
```json
{
		"To": [
				"+441234567890",
				"+441234567891"
		],
		"Content": "Hello, world!"
}
```

# Contributing
Contributions are welcome.  Please fork the repository, make your changes, and 
submit a pull request.  By raising a Pull Request against this repo, you agree
to the terms in the [Contributor Licence Agreement](CLA.md).

Please ensure this document is updated where relevant, and unit tests to cover
issues that are being resolved to prevent future regressions.

# Further documentation

Useful documentation on the Huawei API:

* https://blog.hqcodeshop.fi/archives/259-Huawei-E5186-AJAX-API.html
* https://www.mrt-prodz.com/blog/view/2015/05/huawei-modem-api-and-data-plan-monitor/


# Licence
This code is licensed under the AGPL.  See the LICENSE file in the repository root.

You are free to download and use this application as you please, but if you modify
the code, and make it available to users over a network or distribute it, you must
make the source code available (and accessible) under the same license.

Copyright © 2024. Alastair Grant.<br />
https://www.aligrant.com/

# Support and warranty

This software is provided "as is", without warranty of any kind, express or implied,
including but not limited to the warranties of merchantability, fitness for a particular
purpose and noninfringement. In no event shall the authors or copyright holders be liable
for any claim, damages or other liability, whether in an action of contract, tort or otherwise,
arising from, out of or in connection with the software or the use or other dealings in the
software.

That said, please raise issues and PRs against the project as appropriate.

# Disclaimer
This project is an independent open?source initiative and is not affiliated with, 
endorsed by, or sponsored by Huawei or any related trademarks or entities. 

See [THIRDPARTY-NOTICES.TXT](THIRDPARTY-NOTICES.md) for licences and notices
from third-party libraries.