ASIO output is opt-in and available on x86 and x64 Windows. Install the interface manufacturer's ASIO driver, set `AudioUseAsio` to `true`, and select its name with `AudioAsioDevice`. An empty driver name selects the first installed driver. `AudioAsioSampleRate` is in Hz; `AudioAsioBufferSize` is in samples, with `0` using the driver's default. Unsupported settings fall back to the regular output device and report the reason through `AudioManager.AsioStatus`.

The first two ASIO output channels carry stereo audio. Existing track and sample mixers feed a float decoding mixer, which BASSASIO pulls directly. The driver's output latency is supplied to BASSmix for playback-position reporting. Reset and sample-rate notifications schedule reinitialisation on the audio thread. After reconnecting an unavailable interface, applications can call `AudioManager.RetryAsio()`.

Some drivers take exclusive access to their interface. Output is not automatically released when the application loses focus. Other architectures and platforms continue using the regular audio backend.

The bundled native libraries are unmodified BASSASIO 1.4 binaries from [Un4seen Developments](https://www.un4seen.com/bassasio.html). Their licence is in `LICENCE.BASSASIO.txt`. The source archive is [bassasio14.zip](https://www.un4seen.com/files/bassasio14.zip), SHA-256 `54bfe2f051338bb016b4ca08f840b93e72ee7edfc9bf0245f08d7edc6c72f45d`.

For the optional hardware test, set `OSU_ASIO_TEST_DRIVER` to the installed driver name. It uses 48000 Hz and the driver's default buffer. Set `OSU_ASIO_TEST_BUFFER` to test a specific buffer length. The test writes silence, checks nested-mixer playback positions and latency, and verifies cleanup after failed initialisation and repeated starts.
