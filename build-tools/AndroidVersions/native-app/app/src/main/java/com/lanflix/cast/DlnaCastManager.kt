package com.lanflix.cast

import android.content.Context
import android.net.wifi.WifiManager
import android.util.Log
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress
import java.net.URL
import java.util.concurrent.TimeUnit

data class DlnaDevice(
    val id: String,
    val name: String,
    val locationUrl: String,
    var controlUrl: String = "",
    val manufacturer: String = ""
)

class DlnaCastManager(private val context: Context) {
    private val TAG = "DlnaCastManager"

    private val httpClient = OkHttpClient.Builder()
        .connectTimeout(5, TimeUnit.SECONDS)
        .readTimeout(5, TimeUnit.SECONDS)
        .build()

    private val _discoveredDevices = MutableStateFlow<List<DlnaDevice>>(emptyList())
    val discoveredDevices: StateFlow<List<DlnaDevice>> = _discoveredDevices.asStateFlow()

    private val _activeDevice = MutableStateFlow<DlnaDevice?>(null)
    val activeDevice: StateFlow<DlnaDevice?> = _activeDevice.asStateFlow()

    private val _isCasting = MutableStateFlow(false)
    val isCasting: StateFlow<Boolean> = _isCasting.asStateFlow()

    private val _currentMediaTitle = MutableStateFlow<String?>(null)
    val currentMediaTitle: StateFlow<String?> = _currentMediaTitle.asStateFlow()

    private val _isPlayingOnTv = MutableStateFlow(false)
    val isPlayingOnTv: StateFlow<Boolean> = _isPlayingOnTv.asStateFlow()

    /**
     * Discover Smart TVs (Samsung, LG, Sony, Fire TV, Roku, DLNA Renderers) on local Wi-Fi via SSDP
     */
    suspend fun discoverDevices(timeoutMs: Long = 4000L) = withContext(Dispatchers.IO) {
        val wifiManager = context.applicationContext.getSystemService(Context.WIFI_SERVICE) as? WifiManager
        val multicastLock = wifiManager?.createMulticastLock("LanflixDlnaCastLock")?.apply {
            setReferenceCounted(true)
            acquire()
        }

        val foundMap = mutableMapOf<String, DlnaDevice>()

        try {
            val ssdpSearch = ("M-SEARCH * HTTP/1.1\r\n" +
                    "HOST: 239.255.255.250:1900\r\n" +
                    "MAN: \"ssdp:discover\"\r\n" +
                    "MX: 3\r\n" +
                    "ST: urn:schemas-upnp-org:device:MediaRenderer:1\r\n" +
                    "\r\n").toByteArray(Charsets.UTF_8)

            val group = InetAddress.getByName("239.255.255.250")
            val socket = DatagramSocket()
            socket.soTimeout = timeoutMs.toInt()

            val packet = DatagramPacket(ssdpSearch, ssdpSearch.size, group, 1900)
            socket.send(packet)

            // Also search for all SSDP root devices as fallback
            val ssdpAll = ("M-SEARCH * HTTP/1.1\r\n" +
                    "HOST: 239.255.255.250:1900\r\n" +
                    "MAN: \"ssdp:discover\"\r\n" +
                    "MX: 3\r\n" +
                    "ST: ssdp:all\r\n" +
                    "\r\n").toByteArray(Charsets.UTF_8)
            socket.send(DatagramPacket(ssdpAll, ssdpAll.size, group, 1900))

            val startTime = System.currentTimeMillis()
            val buf = ByteArray(2048)

            while (System.currentTimeMillis() - startTime < timeoutMs) {
                try {
                    val recvPacket = DatagramPacket(buf, buf.size)
                    socket.receive(recvPacket)
                    val response = String(recvPacket.data, 0, recvPacket.length, Charsets.UTF_8)

                    val location = parseHeader(response, "LOCATION")
                    if (!location.isNullOrBlank() && !foundMap.containsKey(location)) {
                        val device = fetchDeviceDescription(location)
                        if (device != null) {
                            foundMap[location] = device
                            _discoveredDevices.value = foundMap.values.toList()
                        }
                    }
                } catch (e: java.io.InterruptedIOException) {
                    // Socket timeout
                    break
                } catch (e: Exception) {
                    Log.d(TAG, "SSDP receive loop error", e)
                }
            }

            socket.close()
        } catch (e: Exception) {
            Log.e(TAG, "SSDP discovery failed", e)
        } finally {
            multicastLock?.release()
        }
    }

    private fun parseHeader(response: String, headerName: String): String? {
        val lines = response.split("\r\n", "\n")
        for (line in lines) {
            val parts = line.split(":", limit = 2)
            if (parts.size == 2 && parts[0].trim().equals(headerName, ignoreCase = true)) {
                return parts[1].trim()
            }
        }
        return null
    }

    private fun fetchDeviceDescription(locationUrl: String): DlnaDevice? {
        return try {
            val req = Request.Builder().url(locationUrl).build()
            val res = httpClient.newCall(req).execute()
            if (!res.isSuccessful) return null
            val xml = res.body?.string() ?: return null

            val friendlyName = extractXmlTag(xml, "friendlyName") ?: "Smart TV"
            val manufacturer = extractXmlTag(xml, "manufacturer") ?: ""

            // Find AVTransport control URL
            var controlUrl = extractAvTransportControlUrl(xml) ?: ""
            if (controlUrl.isNotBlank() && !controlUrl.startsWith("http")) {
                val baseUrl = URL(locationUrl)
                controlUrl = if (controlUrl.startsWith("/")) {
                    "${baseUrl.protocol}://${baseUrl.host}:${baseUrl.port}$controlUrl"
                } else {
                    "${baseUrl.protocol}://${baseUrl.host}:${baseUrl.port}/${controlUrl}"
                }
            }

            if (controlUrl.isNotBlank()) {
                DlnaDevice(
                    id = locationUrl,
                    name = friendlyName,
                    locationUrl = locationUrl,
                    controlUrl = controlUrl,
                    manufacturer = manufacturer
                )
            } else null
        } catch (e: Exception) {
            Log.d(TAG, "Error fetching device description from $locationUrl", e)
            null
        }
    }

    private fun extractXmlTag(xml: String, tagName: String): String? {
        val startTag = "<$tagName>"
        val endTag = "</$tagName>"
        val start = xml.indexOf(startTag, ignoreCase = true)
        if (start == -1) return null
        val end = xml.indexOf(endTag, start, ignoreCase = true)
        if (end == -1) return null
        return xml.substring(start + startTag.length, end).trim()
    }

    private fun extractAvTransportControlUrl(xml: String): String? {
        val avTransportIndex = xml.indexOf("urn:schemas-upnp-org:service:AVTransport:1")
        if (avTransportIndex == -1) return null

        val controlUrlTag = "<controlURL>"
        val controlUrlStart = xml.indexOf(controlUrlTag, avTransportIndex)
        if (controlUrlStart == -1) return null

        val controlUrlEnd = xml.indexOf("</controlURL>", controlUrlStart)
        if (controlUrlEnd == -1) return null

        return xml.substring(controlUrlStart + controlUrlTag.length, controlUrlEnd).trim()
    }

    /**
     * Cast media natively to the selected Smart TV
     */
    suspend fun castMedia(device: DlnaDevice, mediaUrl: String, title: String, startPositionSec: Long = 0L) = withContext(Dispatchers.IO) {
        try {
            _activeDevice.value = device
            _currentMediaTitle.value = title

            // 1. Stop existing playback
            sendSoapAction(device.controlUrl, "Stop", getStopXml())

            // 2. Set URI
            val setUriXml = getSetUriXml(mediaUrl, title)
            val uriSuccess = sendSoapAction(device.controlUrl, "SetAVTransportURI", setUriXml)

            if (!uriSuccess) {
                Log.e(TAG, "SetAVTransportURI failed for ${device.name}")
                return@withContext
            }

            // 3. Optional Seek
            if (startPositionSec > 0) {
                val timeStr = formatTime(startPositionSec)
                sendSoapAction(device.controlUrl, "Seek", getSeekXml(timeStr))
            }

            // 4. Play
            val playSuccess = sendSoapAction(device.controlUrl, "Play", getPlayXml())
            if (playSuccess) {
                _isCasting.value = true
                _isPlayingOnTv.value = true
                Log.i(TAG, "Successfully started native casting to ${device.name}")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Failed to cast media to ${device.name}", e)
        }
    }

    suspend fun play() = withContext(Dispatchers.IO) {
        val device = _activeDevice.value ?: return@withContext
        if (sendSoapAction(device.controlUrl, "Play", getPlayXml())) {
            _isPlayingOnTv.value = true
        }
    }

    suspend fun pause() = withContext(Dispatchers.IO) {
        val device = _activeDevice.value ?: return@withContext
        if (sendSoapAction(device.controlUrl, "Pause", getPauseXml())) {
            _isPlayingOnTv.value = false
        }
    }

    suspend fun stopCasting() = withContext(Dispatchers.IO) {
        val device = _activeDevice.value
        if (device != null) {
            sendSoapAction(device.controlUrl, "Stop", getStopXml())
        }
        _activeDevice.value = null
        _isCasting.value = false
        _isPlayingOnTv.value = false
        _currentMediaTitle.value = null
    }

    private fun sendSoapAction(controlUrl: String, actionName: String, xmlPayload: String): Boolean {
        return try {
            val body = xmlPayload.toRequestBody("text/xml; charset=\"utf-8\"".toMediaType())
            val req = Request.Builder()
                .url(controlUrl)
                .addHeader("SOAPACTION", "\"urn:schemas-upnp-org:service:AVTransport:1#$actionName\"")
                .post(body)
                .build()

            val res = httpClient.newCall(req).execute()
            val ok = res.isSuccessful || res.code == 200
            res.close()
            ok
        } catch (e: Exception) {
            Log.e(TAG, "SOAP Action $actionName failed to $controlUrl", e)
            false
        }
    }

    private fun formatTime(seconds: Long): String {
        val h = seconds / 3600
        val m = (seconds % 3600) / 60
        val s = seconds % 60
        return String.format("%02d:%02d:%02d", h, m, s)
    }

    private fun getSetUriXml(uri: String, title: String): String {
        val escapedTitle = title.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
        val didl = "&lt;DIDL-Lite xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\"&gt;&lt;item id=\"1\" parentID=\"0\" restricted=\"1\"&gt;&lt;dc:title&gt;$escapedTitle&lt;/dc:title&gt;&lt;upnp:class&gt;object.item.videoItem&lt;/upnp:class&gt;&lt;/item&gt;&lt;/DIDL-Lite&gt;"
        return """<?xml version="1.0" encoding="utf-8"?>
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/" s:encodingStyle="http://schemas.xmlsoap.org/soap/encoding/">
  <s:Body>
    <u:SetAVTransportURI xmlns:u="urn:schemas-upnp-org:service:AVTransport:1">
      <InstanceID>0</InstanceID>
      <CurrentURI>$uri</CurrentURI>
      <CurrentURIMetaData>$didl</CurrentURIMetaData>
    </u:SetAVTransportURI>
  </s:Body>
</s:Envelope>"""
    }

    private fun getPlayXml(): String {
        return """<?xml version="1.0" encoding="utf-8"?>
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/" s:encodingStyle="http://schemas.xmlsoap.org/soap/encoding/">
  <s:Body>
    <u:Play xmlns:u="urn:schemas-upnp-org:service:AVTransport:1">
      <InstanceID>0</InstanceID>
      <Speed>1</Speed>
    </u:Play>
  </s:Body>
</s:Envelope>"""
    }

    private fun getPauseXml(): String {
        return """<?xml version="1.0" encoding="utf-8"?>
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/" s:encodingStyle="http://schemas.xmlsoap.org/soap/encoding/">
  <s:Body>
    <u:Pause xmlns:u="urn:schemas-upnp-org:service:AVTransport:1">
      <InstanceID>0</InstanceID>
    </u:Pause>
  </s:Body>
</s:Envelope>"""
    }

    private fun getStopXml(): String {
        return """<?xml version="1.0" encoding="utf-8"?>
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/" s:encodingStyle="http://schemas.xmlsoap.org/soap/encoding/">
  <s:Body>
    <u:Stop xmlns:u="urn:schemas-upnp-org:service:AVTransport:1">
      <InstanceID>0</InstanceID>
    </u:Stop>
  </s:Body>
</s:Envelope>"""
    }

    private fun getSeekXml(relTime: String): String {
        return """<?xml version="1.0" encoding="utf-8"?>
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/" s:encodingStyle="http://schemas.xmlsoap.org/soap/encoding/">
  <s:Body>
    <u:Seek xmlns:u="urn:schemas-upnp-org:service:AVTransport:1">
      <InstanceID>0</InstanceID>
      <Unit>REL_TIME</Unit>
      <Target>$relTime</Target>
    </u:Seek>
  </s:Body>
</s:Envelope>"""
    }
}
