# Kancelarijski senzor - protokol i arhitektura

## Skica sistema

```mermaid
flowchart LR
    Client[SensorClient CSV reader] -->|StartSession(meta), PushSample(sample), EndSession| Service[WCF netTcpBinding service]
    Service --> Storage[(Disk storage: measurements_session.csv, rejects.csv, server_events.log)]
```

## Pravila protokola

1. Svaka sesija pocinje porukom `StartSession(meta)` i meta-zaglavljem `{ Volume, T_DHT, T_BMP, Pressure, DateTime }`.
2. Klijent cita CSV dataset, parsira vrednosti koristeci `InvariantCulture` i sekvencijalno salje najvise prvih 100 validnih redova.
3. Svaki red se salje zasebno kroz `PushSample(sample)`, a sesija se zatvara pozivom `EndSession()`.
4. Servis vraca `SensorResponse` sa `Ack`, `Status` (`IN_PROGRESS` ili `COMPLETED`) i porukom. Validacione greske se vracaju kao `DataFormatFault` ili `ValidationFault`.
5. Pragovi su u konfiguraciji: `V_threshold`, `T_bmp_threshold`, `T_dht_threshold` i `MeanDeviationPercent` za odstupanje od tekuceg proseka.

## Obrada na serveru

- `StartSession` kreira novi direktorijum sesije, `measurements_session.csv` i `rejects.csv`.
- `PushSample` validira obavezna polja i opsege, upisuje validne uzorke i podize dogadjaje `OnSampleReceived` i `OnWarningRaised`.
- `EndSession` zatvara fajlove kroz `IDisposable` i podize `OnTransferCompleted`.
- `CsvSessionWriter` i `CsvDatasetReader` zatvaraju `FileStream`, `StreamReader` i `StreamWriter` kroz `Dispose`; u slucaju prekida prenosa servis zatvara writer pre vracanja fault-a.

## Dogadjaji i analitika

- `OnTransferStarted` zapisuje pocetak prenosa.
- `OnSampleReceived` zapisuje svaki primljeni uzorak.
- `OnTransferCompleted` zapisuje kraj prenosa.
- `OnWarningRaised` se koristi za `VolumeSpike`, `OutOfBandWarning`, `TemperatureSpikeDHT` i `TemperatureSpikeBMP`.
