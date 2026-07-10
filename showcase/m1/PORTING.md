# slice-zero — Port Atlas (KP-0 floor)

- Direction: rust->csharp
- Source root: C:\Work\FireHorseCoding\fixtures\slice-zero\rust
- Target root: C:\Work\FireHorseCoding\fixtures\slice-zero\csharp

## Policy

- kp-default@1
  - autoAccept.kpBehavior: false
  - autoAccept.kpVerification: true

## Units

### parser-core — Header parser core
- status: mapped (stale)
- behavior:parser-core:B1: accepted (stale)
- behavior:parser-core:B2: accepted
- behavior:parser-core:B3: proposed (stale)

## Correspondences

- corr-adapt-result [adapts] unit=parser-core kp.correspondence claim=proposed (stale)
- corr-covers [covers] unit=parser-core kp.correspondence claim=proposed
- corr-errorcode [maps-to] unit=parser-core kp.correspondence claim=proposed
- corr-implements [implements] unit=parser-core kp.correspondence claim=proposed (stale)
- corr-parse [maps-to] unit=parser-core kp.correspondence claim=proposed (stale)

## Claims

### Behavior (kp.behavior)

- behavior:parser-core:B1 -> accepted (stale)
- behavior:parser-core:B2 -> accepted
- behavior:parser-core:B3 -> proposed (stale)

### Correspondence (kp.correspondence)

- corr:corr-adapt-result -> proposed (stale)
- corr:corr-covers -> proposed
- corr:corr-errorcode -> proposed
- corr:corr-implements -> proposed (stale)
- corr:corr-parse -> proposed (stale)

### Verification (kp.verification)

- verify:parser-core:io-agreement-v1 -> accepted (stale)

