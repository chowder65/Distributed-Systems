#!/usr/bin/env bash
# Exercises every RetroGameExchange endpoint end to end.
set -e

BASE="${1:-http://localhost:8080}"

extract_token() {
    sed -n 's/.*"token":"\([^"]*\)".*/\1/p'
}

echo "-- register alice --"
curl -s -X POST "$BASE/api/users" \
  -H "Content-Type: application/json" \
  -d '{"name":"Alice","email":"alice@example.com","password":"secret12","streetAddress":"1 Main St"}'
echo

echo "-- register bob --"
curl -s -X POST "$BASE/api/users" \
  -H "Content-Type: application/json" \
  -d '{"name":"Bob","email":"bob@example.com","password":"secret12","streetAddress":"2 Oak Ave"}'
echo

ALICE_TOKEN=$(curl -s -X POST "$BASE/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"secret12"}' | extract_token)

BOB_TOKEN=$(curl -s -X POST "$BASE/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"bob@example.com","password":"secret12"}' | extract_token)

echo "-- get alice --"
curl -s "$BASE/api/users/1" -H "Authorization: Bearer $ALICE_TOKEN"
echo

echo "-- update alice --"
curl -s -X PUT "$BASE/api/users/1" \
  -H "Authorization: Bearer $ALICE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Alice R. Trader","streetAddress":"456 Oak Ave"}'
echo

echo "-- alice creates a game --"
curl -s -X POST "$BASE/api/games" \
  -H "Authorization: Bearer $ALICE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Chrono Trigger","publisher":"Square","yearPublished":1995,"gamingSystem":"SNES","condition":"good","previousOwners":1}'
echo

echo "-- bob creates a game --"
curl -s -X POST "$BASE/api/games" \
  -H "Authorization: Bearer $BOB_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Metroid","publisher":"Nintendo","yearPublished":1986,"gamingSystem":"NES","condition":"fair"}'
echo

echo "-- search games by name --"
curl -s "$BASE/api/games?name=Mario" -H "Authorization: Bearer $ALICE_TOKEN"
echo

echo "-- get game 1 --"
curl -s "$BASE/api/games/1" -H "Authorization: Bearer $ALICE_TOKEN"
echo

echo "-- update game 1 --"
curl -s -X PUT "$BASE/api/games/1" \
  -H "Authorization: Bearer $ALICE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Chrono Trigger","publisher":"Square","yearPublished":1995,"gamingSystem":"SNES","condition":"mint","previousOwners":1}'
echo

echo "-- bob offers his Metroid (game 2) for alice's Chrono Trigger (game 1) --"
curl -s -X POST "$BASE/api/trade-offers" \
  -H "Authorization: Bearer $BOB_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"offeredGameId":2,"requestedGameId":1}'
echo

echo "-- alice views her incoming pending offers --"
curl -s "$BASE/api/trade-offers?direction=incoming&status=pending" -H "Authorization: Bearer $ALICE_TOKEN"
echo

echo "-- get trade offer 1 --"
curl -s "$BASE/api/trade-offers/1" -H "Authorization: Bearer $ALICE_TOKEN"
echo

echo "-- alice accepts the offer --"
curl -s -X PUT "$BASE/api/trade-offers/1" \
  -H "Authorization: Bearer $ALICE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status":"accepted"}'
echo

echo "-- delete game 1 --"
curl -s -X DELETE "$BASE/api/games/1" -H "Authorization: Bearer $ALICE_TOKEN"
echo

echo "-- load-balancer check: 6 requests, then nginx upstream log --"
for i in 1 2 3 4 5 6; do
    curl -s "$BASE/api/users/1" -H "Authorization: Bearer $ALICE_TOKEN" >/dev/null
done
docker logs lab1-nginx-1 --tail 6 2>&1 || true
