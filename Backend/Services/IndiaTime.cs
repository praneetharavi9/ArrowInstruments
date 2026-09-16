namespace Backend.Services
{
    // The app has no per-admin timezone setting — every date/time an admin
    // enters (ledger entry dates, reminder send times) is a naive wall-clock
    // value in India Standard Time (UTC+5:30, no DST), matching where
    // ArrowInstruments and its admin operator are. This converts between that
    // convention and the true UTC instants .NET/the OS clock use, so
    // time-sensitive comparisons (like "is this reminder due yet?") line up
    // correctly instead of comparing an IST wall-clock value against real UTC.
    public static class IndiaTime
    {
        public static readonly TimeSpan Offset = TimeSpan.FromHours(5.5);

        // "Now", expressed as the same naive IST wall-clock shape that
        // admin-entered date/time fields are stored in. Kind is explicitly
        // Unspecified (not Utc, which DateTime.UtcNow + Offset would otherwise
        // carry) — the value is IST, not UTC, and a Utc-tagged value would
        // serialize to JSON with a trailing "Z", causing the browser to
        // re-apply its own timezone offset on top of the one already added here.
        public static DateTime NowAsIst => DateTime.SpecifyKind(DateTime.UtcNow + Offset, DateTimeKind.Unspecified);
    }
}
