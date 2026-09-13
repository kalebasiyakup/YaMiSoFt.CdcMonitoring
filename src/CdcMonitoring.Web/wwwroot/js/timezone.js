export function getTimeZoneId() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}
