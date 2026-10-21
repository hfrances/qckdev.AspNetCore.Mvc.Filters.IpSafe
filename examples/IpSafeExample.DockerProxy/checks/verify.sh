#!/bin/sh
set -eu

wait_for_api() {
    base_url="$1"
    attempt=0
    while [ "$attempt" -lt 30 ]; do
        status=$(curl --silent --output /dev/null --write-out '%{http_code}' \
            --connect-timeout 2 --max-time 3 "$base_url/ipsafe/public") || status=000
        if [ "$status" = 200 ]; then
            return
        fi
        attempt=$((attempt + 1))
        sleep 1
    done
    printf 'FAIL: %s did not become ready\n' "$base_url" >&2
    exit 1
}

check() {
    expected="$1"
    url="$2"
    shift 2
    actual=$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
        --connect-timeout 3 --max-time 10 "$@" "$url")
    if [ "$actual" != "$expected" ]; then
        printf 'FAIL: %s %s expected %s, received %s\n' "$url" "$*" "$expected" "$actual" >&2
        exit 1
    fi
    printf 'PASS: %s %s %s\n' "$actual" "$url" "$*"
}

wait_for_api http://nginx:8080
wait_for_api http://api:8080

check 200 http://nginx:8080/ipsafe/public
check 200 http://nginx:8080/ipsafe/office
check 403 http://nginx:8080/ipsafe/partner

# Neither scheme's allowed IP can be spoofed by an untrusted caller.
for spoofed_ip in 198.51.100.10 198.51.100.20; do
    check 200 http://api:8080/ipsafe/public -H "X-Forwarded-For: $spoofed_ip"
    check 403 http://api:8080/ipsafe/office -H "X-Forwarded-For: $spoofed_ip"
    check 403 http://api:8080/ipsafe/partner -H "X-Forwarded-For: $spoofed_ip"
done

printf 'All 9 checks passed.\n'
