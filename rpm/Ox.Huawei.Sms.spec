%global have_dotnet10 %(dotnet --version 2>/dev/null | grep -q '^10\.' && echo 1 || echo 0)

#
# spec file for package Ox.Huawei.Sms
#

Name:           smsmonitor
Version:        1.0.0
Release:        %{?release: %{release}}%{!?release:test}%{?dist}

Summary:        SMS Monitoring tools for Huawei USB dongles
License:        AGPL-3.0-only
URL:            https://www.aligrant.com/
Source0:        ox.huawei.sms_%{VERSION}.tar.gz
%if 0%{?have_dotnet10} == 0
BuildRequires: dotnet-sdk-10.0
%endif

# GitHub runners don't have the systemd rpm macros
%{!?_unitdir: %global _unitdir /usr/lib/systemd/system}

%description
Ox.Huawei.Sms is a toolset for the monitoring of USB, router-mode Huawei dongles
for incoming text/sms messages, to be forwarded to MQTT or SMTP locations.
Including a CLI utility to send new messages on said device.

%prep
# Copy all files from Source0 to the build directory
%setup -c

%build
dotnet publish ox.huawei.sms-%{version}/Ox.Huawei.Sms.Monitor/Ox.Huawei.Sms.Monitor.csproj -c Release -p:DebugSymbols=false -p:PublishProfile=LinuxSelfContained -p:SourceRevisionId=%{commit} -o ./publish
dotnet publish ox.huawei.sms-%{version}/Ox.Huawei.Sms.SendCli/Ox.Huawei.Sms.SendCli.csproj -c Release -p:DebugSymbols=false -p:PublishProfile=LinuxSelfContained -p:SourceRevisionId=%{commit} -o ./publish

%install
# Copy published binaries and files
install -p -D -m 0755 publish/smsmonitor %{buildroot}%{_sbindir}/smsmonitor
install -p -D -m 0755 publish/sendsms %{buildroot}%{_bindir}/sendsms
install -p -D -m 0640 publish/appsettings.json %{buildroot}/etc/smsmonitor.json
install -p -D -m 0644 publish/smsmonitor.service %{buildroot}%{_unitdir}/smsmonitor.service

%files
%{_sbindir}/smsmonitor
%{_bindir}/sendsms
%{_unitdir}
%attr (0640,-,-) %config(noreplace) /etc/smsmonitor.json
%license ox.huawei.sms-%{version}/LICENSE.txt
%doc ox.huawei.sms-%{version}/Ox.Huawei.Sms.Monitor/docs/*.man
%doc ox.huawei.sms-%{version}/Ox.Huawei.Sms.Monitor/docs/INSTALL.txt
%doc ox.huawei.sms-%{version}/Ox.Huawei.Sms.SendCli/docs/*.man

%changelog
