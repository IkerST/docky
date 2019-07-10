//  
//  Copyright (C) 2009 Chris Szikszoy
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;

using DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	[Interface ("org.freedesktop.NetworkManager.Device")]
	public interface INetworkDevice : org.freedesktop.DBus.Properties
	{
		event StateChangedHandler StateChanged;
	}
	
	public delegate void StateChangedHandler(DeviceState new_state, DeviceState old_state, uint reason);
	
	//we can leave reason as a uint for now.  I don't really see a need to make an enum for the reason, but the response codes are shown below:
	/*
		NM_DEVICE_STATE_REASON_UNKNOWN = 0
		    The reason for the device state change is unknown. 
		NM_DEVICE_STATE_REASON_NONE = 1
		    The state change is normal. 
		NM_DEVICE_STATE_REASON_NOW_MANAGED = 2
		    The device is now managed. 
		NM_DEVICE_STATE_REASON_NOW_UNMANAGED = 3
		    The device is no longer managed. 
		NM_DEVICE_STATE_REASON_CONFIG_FAILED = 4
		    The device could not be readied for configuration. 
		NM_DEVICE_STATE_REASON_CONFIG_UNAVAILABLE = 5
		    IP configuration could not be reserved (no available address, timeout, etc). 
		NM_DEVICE_STATE_REASON_CONFIG_EXPIRED = 6
		    The IP configuration is no longer valid. 
		NM_DEVICE_STATE_REASON_NO_SECRETS = 7
		    Secrets were required, but not provided. 
		NM_DEVICE_STATE_REASON_SUPPLICANT_DISCONNECT = 8
		    The 802.1X supplicant disconnected from the access point or authentication server. 
		NM_DEVICE_STATE_REASON_SUPPLICANT_CONFIG_FAILED = 9
		    Configuration of the 802.1X supplicant failed. 
		NM_DEVICE_STATE_REASON_SUPPLICANT_FAILED = 10
		    The 802.1X supplicant quit or failed unexpectedly. 
		NM_DEVICE_STATE_REASON_SUPPLICANT_TIMEOUT = 11
		    The 802.1X supplicant took too long to authenticate. 
		NM_DEVICE_STATE_REASON_PPP_START_FAILED = 12
		    The PPP service failed to start within the allowed time. 
		NM_DEVICE_STATE_REASON_PPP_DISCONNECT = 13
		    The PPP service disconnected unexpectedly. 
		NM_DEVICE_STATE_REASON_PPP_FAILED = 14
		    The PPP service quit or failed unexpectedly. 
		NM_DEVICE_STATE_REASON_DHCP_START_FAILED = 15
		    The DHCP service failed to start within the allowed time. 
		NM_DEVICE_STATE_REASON_DHCP_ERROR = 16
		    The DHCP service reported an unexpected error. 
		NM_DEVICE_STATE_REASON_DHCP_FAILED = 17
		    The DHCP service quit or failed unexpectedly. 
		NM_DEVICE_STATE_REASON_SHARED_START_FAILED = 18
		    The shared connection service failed to start. 
		NM_DEVICE_STATE_REASON_SHARED_FAILED = 19
		    The shared connection service quit or failed unexpectedly. 
		NM_DEVICE_STATE_REASON_AUTOIP_START_FAILED = 20
		    The AutoIP service failed to start. 
		NM_DEVICE_STATE_REASON_AUTOIP_ERROR = 21
		    The AutoIP service reported an unexpected error. 
		NM_DEVICE_STATE_REASON_AUTOIP_FAILED = 22
		    The AutoIP service quit or failed unexpectedly. 
		NM_DEVICE_STATE_REASON_MODEM_BUSY = 23
		    Dialing failed because the line was busy. 
		NM_DEVICE_STATE_REASON_MODEM_NO_DIAL_TONE = 24
		    Dialing failed because there was no dial tone. 
		NM_DEVICE_STATE_REASON_MODEM_NO_CARRIER = 25
		    Dialing failed because there was carrier. 
		NM_DEVICE_STATE_REASON_MODEM_DIAL_TIMEOUT = 26
		    Dialing timed out. 
		NM_DEVICE_STATE_REASON_MODEM_DIAL_FAILED = 27
		    Dialing failed. 
		NM_DEVICE_STATE_REASON_MODEM_INIT_FAILED = 28
		    Modem initialization failed. 
		NM_DEVICE_STATE_REASON_GSM_APN_FAILED = 29
		    Failed to select the specified GSM APN. 
		NM_DEVICE_STATE_REASON_GSM_REGISTRATION_NOT_SEARCHING = 30
		    Not searching for networks. 
		NM_DEVICE_STATE_REASON_GSM_REGISTRATION_DENIED = 31
		    Network registration was denied. 
		NM_DEVICE_STATE_REASON_GSM_REGISTRATION_TIMEOUT = 32
		    Network registration timed out. 
		NM_DEVICE_STATE_REASON_GSM_REGISTRATION_FAILED = 33
		    Failed to register with the requested GSM network. 
		NM_DEVICE_STATE_REASON_GSM_PIN_CHECK_FAILED = 34
		    PIN check failed. 
		NM_DEVICE_STATE_REASON_FIRMWARE_MISSING = 35
		    Necessary firmware for the device may be missing. 
		NM_DEVICE_STATE_REASON_REMOVED = 36
		    The device was removed. 
		NM_DEVICE_STATE_REASON_SLEEPING = 37
		    NetworkManager went to sleep. 
		NM_DEVICE_STATE_REASON_CONNECTION_REMOVED = 38
		    The device's active connection was removed or disappeared. 
		NM_DEVICE_STATE_REASON_USER_REQUESTED = 39
		    A user or client requested the disconnection. 
		NM_DEVICE_STATE_REASON_CARRIER = 40
		    The device's carrier/link changed. 
    */
}
