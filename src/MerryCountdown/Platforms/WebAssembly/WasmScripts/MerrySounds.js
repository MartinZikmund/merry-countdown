// Short sound effects through Web Audio. An <audio> element reopens its file on every play; a decoded
// buffer starts on the next audio frame. Called from Platforms/WebAssembly/PlatformSoundOutput.cs.
(function () {
    const buffers = {};
    let context = null;

    function getContext() {
        context ??= new (globalThis.AudioContext || globalThis.webkitAudioContext)();
        return context;
    }

    globalThis.MerrySounds = {
        load(name, url) {
            fetch(url)
                .then(response => response.ok ? response.arrayBuffer() : Promise.reject(new Error(`${response.status} ${url}`)))
                .then(data => getContext().decodeAudioData(data))
                .then(buffer => { buffers[name] = buffer; })
                .catch(error => console.warn(`MerrySounds: couldn't load ${name}`, error));
        },

        play(name) {
            const buffer = buffers[name];
            if (!buffer) {
                return;
            }

            // Browsers keep audio suspended until the first tap. This call is that tap.
            const audio = getContext();
            if (audio.state === "suspended") {
                audio.resume();
            }

            const source = audio.createBufferSource();
            source.buffer = buffer;
            source.connect(audio.destination);
            source.start();
        }
    };
})();
