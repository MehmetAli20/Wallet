local key = KEYS[1]

local capacity = tonumber(ARGV[1])
local refill_per_second = tonumber(ARGV[2])
local cost = tonumber(ARGV[3])
local ttl = tonumber(ARGV[4])

local time = redis.call('TIME')
local now = tonumber(time[1]) + tonumber(time[2]) / 1000000

local bucket = redis.call('HMGET', key, 'tokens', 'updated')
local tokens = tonumber(bucket[1])
local updated = tonumber(bucket[2])

if tokens == nil then
    tokens = capacity
    updated = now
end

local elapsed = now - updated

if elapsed > 0 then
    tokens = math.min(capacity, tokens + elapsed * refill_per_second)
end

local allowed = 0
local retry_after = 0

if tokens >= cost then
    tokens = tokens - cost
    allowed = 1
else
    retry_after = math.ceil((cost - tokens) / refill_per_second)
end

redis.call('HSET', key, 'tokens', tokens, 'updated', now)
redis.call('EXPIRE', key, ttl)

return { allowed, math.floor(tokens), retry_after }
