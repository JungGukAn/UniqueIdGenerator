# UniqueIdGenerator

UniqueIdGenerator is a unique id generator library for .NET distributed systems.

Each system can make unique id that is not duplicated globally when has different generator id.

UniqueIdGenerator's unique id is composed of
```
32 bits for seconds passed since specific point
14 bits for a generator id
17 bits for a sequence number
```

As a result, UniqueIdGenerator has the following characteristics.

- It can work for about 130 years
- It can work on 16383 (2^14 -1) distributed systems
- It can generate 131071 (2^17 -1) unique id per second by each system


## Usage
UniqueIdGenerator has one constructor.
```C#
public UniqueIdGenerator(int generatorId)
```

In order to generate new unique id, you can call one of following methods.
```C#
public async Task<UniqueId> IssueAsync()
public async Task<List<UniqueId>> IssueAsync(int requestCount)
```
`Note: These methods work synchronously, except when maximum sequence number is exceeded or run on past than last executed because of time server.`

UniqueId is implicitly converted to long type.
```C#
var uniqueIdGenerator = new UniqueIdGenerator(1);
UniqueId uniqueId = await uniqueIdGenerator.IssueAsync();
long uniqueIdValue = uniqueId;
```

UniqueId also knows when created
```C#
DateTime createdAt = uniqueId.CreatedAt;
```