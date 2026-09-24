using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI transitions as class toggles (durations/easing live in USS) plus a few schedule-driven effects.
/// Every effect is a no-op or instant when the root carries <c>bq-no-motion</c>.
/// </summary>
public static class UiFx
{
    public const string NoMotionClass = "bq-no-motion";
    public const string ReduceMotionPrefKey = "bq_reduce_motion";
    const string EnterClass  = "content-panel--enter";
    const string ExitClass   = "content-panel--exit";
    const string FxClass     = "bq-fx";
    const string PopClass    = "bq-fx--pop";
    const string ShakeL      = "bq-fx--shake-l";
    const string ShakeR      = "bq-fx--shake-r";
    const string FlashClass  = "bq-fx--flash";
    const string BannerIn    = "lower-third--in";

    const int ExitMs = 120, PopHalfMs = 90, ShakeStepMs = 50, ShakeSteps = 6, FlashMs = 400;
    const int BannerSlideMs = 250, BannerHoldMs = 1200;

    // Tracks the in-flight Pop/Shake per element so a new call can cancel the old one
    // instead of leaving a dangling scheduled callback and stuck classes behind.
    static readonly Dictionary<VisualElement, IVisualElementScheduledItem> _running = new();

    static IVisualElementScheduledItem _bannerHide;

    // Tracks a scheduled SwitchPanel enter per panel group (keyed by the shared parent) so a rapid
    // second switch within the same group can cancel a stale Show() that would otherwise land on a
    // target that is no longer current — without cancelling an unrelated group's own pending Show()
    // (e.g. ShowPanel + ShowSessionTab firing back-to-back for two different panel containers).
    static readonly Dictionary<VisualElement, IVisualElementScheduledItem> _pendingShow = new();

    // Tracks elements whose FlashRow fade is still in flight, so ReleaseFx (called by Pop/Shake
    // cleanups that may finish first) doesn't strip bq-fx out from under a still-fading flash.
    static readonly HashSet<VisualElement> _flashing = new();

    /// <summary>True when the element's panel root has reduced motion enabled.</summary>
    public static bool NoMotion(VisualElement el)
    {
        for (var e = el; e != null; e = e.parent)
            if (e.ClassListContains(NoMotionClass)) return true;
        return false;
    }

    public static void SetReducedMotion(VisualElement root, bool reduced)
    {
        if (reduced) root.AddToClassList(NoMotionClass);
        else         root.RemoveFromClassList(NoMotionClass);
    }

    /// <summary>Reads the persisted reduce-motion preference and applies it to <paramref name="root"/>.</summary>
    public static void ApplyPersistedReducedMotion(VisualElement root)
    {
        bool reduced = PlayerPrefs.GetInt(ReduceMotionPrefKey, 0) == 1;
        SetReducedMotion(root, reduced);
    }

    // ── Panels / overlays ────────────────────────────────────────────────────

    /// <summary>Fades <paramref name="from"/> out first (120 ms exit), then slides <paramref name="to"/> in
    /// (220 ms enter) once the exit finishes, so the two panels never overlap and squash the shared flex row.
    /// Switching to the already-visible panel is a no-op.</summary>
    public static void SwitchPanel(VisualElement from, VisualElement to)
    {
        if (from == to) { if (to != null && to.resolvedStyle.display == DisplayStyle.None) Show(to); return; }
        if (from != null) Hide(from);
        if (to == null) return;

        var key = to.parent ?? to;
        if (_pendingShow.TryGetValue(key, out var pending))
        {
            pending.Pause();
            _pendingShow.Remove(key);
        }

        if (from == null || NoMotion(to)) Show(to);
        else _pendingShow[key] = to.schedule.Execute(() =>
        {
            _pendingShow.Remove(key);
            Show(to);
        }).StartingIn(ExitMs);
    }

    /// <summary>display:flex with enter animation (opacity 0 / translate 16px → identity).</summary>
    public static void Show(VisualElement el)
    {
        el.RemoveFromClassList(ExitClass);
        if (NoMotion(el))
        {
            el.RemoveFromClassList(EnterClass);
            el.style.display = DisplayStyle.Flex;
            return;
        }
        el.AddToClassList(EnterClass);          // 0 s → snaps to opacity 0
        el.style.display = DisplayStyle.Flex;
        el.schedule.Execute(() => el.RemoveFromClassList(EnterClass)); // next frame → 220 ms ease-out
    }

    /// <summary>Fade out, then display:none. Cancels if the element gets shown again meanwhile.</summary>
    public static void Hide(VisualElement el)
    {
        if (el.resolvedStyle.display == DisplayStyle.None) return;
        if (NoMotion(el)) { el.style.display = DisplayStyle.None; return; }
        el.AddToClassList(ExitClass);
        el.schedule.Execute(() =>
        {
            if (!el.ClassListContains(ExitClass)) return; // re-shown in the meantime
            el.style.display = DisplayStyle.None;
            el.RemoveFromClassList(ExitClass);
        }).StartingIn(ExitMs);
    }

    // ── Sidebar marker ───────────────────────────────────────────────────────

    /// <summary>Moves the absolute rail marker to <paramref name="target"/>'s row (USS transitions <c>top</c>).</summary>
    public static void MoveNavMarker(VisualElement marker, VisualElement target)
    {
        if (marker == null || target == null) return;
        var r = target.layout;
        if (float.IsNaN(r.y) || r.height <= 0f) return; // layout not ready yet
        marker.style.top    = r.y;
        marker.style.height = r.height;
    }

    // ── Number tick ──────────────────────────────────────────────────────────

    /// <summary>Pure interpolation used by <see cref="TickNumber"/>; t is clamped to [0,1].</summary>
    public static int TickValue(int from, int to, float t)
        => Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.Clamp01(t)));

    /// <summary>Animates the label text from → to over <paramref name="durationMs"/>.</summary>
    public static void TickNumber(Label label, int from, int to, int durationMs = 250)
    {
        if (label == null) return;
        if (from == to || NoMotion(label)) { label.text = to.ToString(); return; }

        long start = -1;
        IVisualElementScheduledItem item = null;
        item = label.schedule.Execute(ts =>
        {
            if (start < 0) start = ts.now;
            float t = (ts.now - start) / (float)durationMs;
            label.text = TickValue(from, to, t).ToString();
            if (t >= 1f) item.Pause();
        }).Every(16);
    }

    // ── Event pulses ─────────────────────────────────────────────────────────

    /// <summary>Cancels an in-flight Pop/Shake on <paramref name="el"/> and clears their classes.</summary>
    static void CancelRunning(VisualElement el)
    {
        if (_running.TryGetValue(el, out var running))
        {
            running.Pause();
            _running.Remove(el);
        }
        el.RemoveFromClassList(PopClass);
        el.RemoveFromClassList(ShakeL);
        el.RemoveFromClassList(ShakeR);
    }

    /// <summary>Removes <c>bq-fx</c> only when no other fx class still needs it — Pop, Shake and FlashRow
    /// share the class on the same element, so whichever finishes last must be the one to drop it.</summary>
    static void ReleaseFx(VisualElement el)
    {
        if (el.ClassListContains(PopClass) || el.ClassListContains(ShakeL) ||
            el.ClassListContains(ShakeR) || el.ClassListContains(FlashClass) ||
            _flashing.Contains(el)) return;
        el.RemoveFromClassList(FxClass);
    }

    /// <summary>scale 1 → 1.06 → 1 (180 ms total).</summary>
    public static void Pop(VisualElement el)
    {
        if (el == null || NoMotion(el)) return;
        CancelRunning(el);
        el.AddToClassList(FxClass);
        el.AddToClassList(PopClass);
        int step = 0;
        IVisualElementScheduledItem item = null;
        item = el.schedule.Execute(() =>
        {
            step++;
            if (step == 1) { el.RemoveFromClassList(PopClass); return; } // 90 ms → ease back starts
            item.Pause();
            ReleaseFx(el);                                              // 180 ms → animation done
            _running.Remove(el);
        }).Every(PopHalfMs);
        _running[el] = item;
    }

    /// <summary>translate ±6 px, 6 steps × 50 ms.</summary>
    public static void Shake(VisualElement el)
    {
        if (el == null || NoMotion(el)) return;
        CancelRunning(el);
        el.AddToClassList(FxClass);
        int step = 0;
        IVisualElementScheduledItem item = null;
        item = el.schedule.Execute(() =>
        {
            el.RemoveFromClassList(ShakeL);
            el.RemoveFromClassList(ShakeR);
            if (step >= ShakeSteps)
            {
                item.Pause();
                ReleaseFx(el);
                _running.Remove(el);
                return;
            }
            el.AddToClassList(step % 2 == 0 ? ShakeL : ShakeR);
            step++;
        }).Every(ShakeStepMs);
        _running[el] = item;
    }

    /// <summary>Instant background highlight that fades over 400 ms; <c>bq-fx</c> is released once the fade
    /// finishes (400 ms), guarded by <see cref="_flashing"/> so a concurrent Pop/Shake cleanup that finishes
    /// earlier can't strip it out from under the still-running fade.</summary>
    public static void FlashRow(VisualElement el)
    {
        if (el == null || NoMotion(el)) return;
        el.AddToClassList(FxClass);
        el.AddToClassList(FlashClass);                                 // 0 s → instant highlight
        _flashing.Add(el);
        el.schedule.Execute(() => el.RemoveFromClassList(FlashClass)); // next frame → 400 ms fade (bq-fx still attached)
        el.schedule.Execute(() =>
        {
            _flashing.Remove(el);
            ReleaseFx(el);                                             // 400 ms → fade done
        }).StartingIn(FlashMs);
    }

    // ── Lower third banner ───────────────────────────────────────────────────

    /// <summary>Slides the root-level lower third in (250 ms), holds 1.2 s, slides out. Hard show/hide under reduced motion.</summary>
    public static void ShowBanner(VisualElement root, string text)
    {
        var banner = root.Q<VisualElement>("lower-third");
        var label  = root.Q<Label>("lower-third-label");
        if (banner == null || label == null) return;

        label.text = text;
        _bannerHide?.Pause();
        banner.AddToClassList(BannerIn);
        int hold = NoMotion(banner) ? BannerHoldMs : BannerSlideMs + BannerHoldMs;
        _bannerHide = banner.schedule.Execute(() => banner.RemoveFromClassList(BannerIn)).StartingIn(hold);
    }
}
