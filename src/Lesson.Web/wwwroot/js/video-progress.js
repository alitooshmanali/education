window.lessonVideoProgress = {
    init: function (videoEl, contentId, resumeSeconds, trackProgress) {
        if (!videoEl || !contentId) return;

        var resume = Number(resumeSeconds) || 0;
        var track = trackProgress !== false;
        var seekDone = false;

        function trySeek() {
            if (seekDone || resume <= 0.5) return;
            var dur = videoEl.duration;
            if (!isFinite(dur) || dur <= 0) return;

            // if completed near end, start from 0 (caller usually passes 0)
            var target = resume;
            if (target >= dur - 1) {
                target = 0;
            }

            try {
                videoEl.currentTime = target;
                seekDone = true;
                console.log("[lessonVideoProgress] resumed at", target, "of", dur);
            } catch (e) {
                console.warn("[lessonVideoProgress] seek failed", e);
            }
        }

        videoEl.addEventListener("loadedmetadata", trySeek);
        videoEl.addEventListener("loadeddata", trySeek);
        videoEl.addEventListener("canplay", trySeek);

        // retries (some browsers delay duration)
        setTimeout(trySeek, 200);
        setTimeout(trySeek, 600);
        setTimeout(trySeek, 1200);

        if (!track) {
            return; // admin: no progress reporting
        }

        var lastSent = -10;

        function report(force) {
            var pos = videoEl.currentTime || 0;
            var dur = videoEl.duration;
            if (!force && Math.abs(pos - lastSent) < 3) return;
            lastSent = pos;

            fetch("/api/contents/" + contentId + "/progress", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "same-origin",
                body: JSON.stringify({
                    positionSeconds: pos,
                    durationSeconds: isFinite(dur) ? dur : null
                })
            }).catch(function () { });
        }

        videoEl.addEventListener("timeupdate", function () { report(false); });
        videoEl.addEventListener("pause", function () { report(true); });
        videoEl.addEventListener("ended", function () {
            lastSent = -10;
            report(true);
        });
    }
};
