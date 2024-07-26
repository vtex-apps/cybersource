# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Changed trigger even for Decison Manager check to Pending Payment
- Added logging

## [1.24.1] - 2024-07-23

### Fixed
- Process Conversions multiple days

## [1.24.0] - 2024-07-23

### Added
- Added check for Decison Manager order updates

## [1.23.0] - 2024-07-12

### Fixed
- Decsion Manager Update Status

## [1.22.3] - 2024-07-09

### Changed
- Improved Timeout logic

## [1.22.2] - 2024-07-08

### Changed
- Reduce calls to VBase
- Improve logging of VBase calls
- Retry GetPaymentData VBase calls

## [1.22.1] - 2024-06-25

### Fixed
- Accept two digit country codes

## [1.22.0] - 2024-06-03

### Fixed
- Do not save error responses to payment data

### Changed
- Updated to apps-graphql 3.x

## [1.21.2] - 2024-04-12

### Fixed
- Allow null country code

## [1.21.1] - 2024-03-18

### Fixed
- Handle case where payment data is not loaded for reversal

## [1.21.0] - 2024-03-01

### Fixed
- Null checks
- Get card type from request not bin lookup

## [1.20.1] - 2024-02-14

### Fixed
- If merchant settings are not loaded from storage, get from request

## [1.20.0] - 2024-01-31

### Changed
- Remove parenthesis from signature header to comply with Cybersource security change 

## [1.19.3] - 2023-12-20

### Fixed
- Set auth response to Denied on error response.
- Changed log for Get Property failure from error to warn.
- Minor bug fixes.

## [1.19.2] - 2023-12-11

### Fixed
- Added catch blocks

## [1.19.1] - 2023-08-28

### Changed
- Use orderformId as fingerprint instead of order id

## [1.19.0] - 2023-07-07

### Added
- Added option to use order id for Device Fingerprint in place of session id

## [1.18.3] - 2023-06-13

### Fixed
- If Auth & Bill is enabled, set Timeout flag by default to force a check for previous captures.

## [1.18.2] - 2023-06-08

### Fixed
- Changed tax calculation to reduce rounding

## [1.18.1] - 2023-05-24

### Fixed
- Fix for account override settings not being used

## [1.18.0] - 2023-05-22

### Changed
- Removed Reversal Reason

## [1.17.0] - 2023-05-12

### Added
- Added Timeout to Create Payment
- If Auth&Bill attempt times out, check transactions in Cybersource before another attempt

## [1.16.1] - 2023-05-04

### Fixed
- Fixed query for Merchant Defined Data settings

## [1.16.0] - 2023-03-29

### Changed
- Use custom query for app settings

## [1.15.0] - 2023-03-28

### Added
- Adjust item tax to match order tax

## [1.14.1] - 2023-03-17

### Changed
- If Decision Manager is not being used, mark pending auths as approved.

## [1.14.0] - 2023-03-16

### Added
- Added option to set settle delay
- Added option to set if Decision Manager is being used so that we can mark pending auths as denied.

### Changed
- For update shipping information, reduce softRetry and set timeout to 8000

### Removed
- [ENGINEERS-1144] - Remove hardcoded cybersource ui version from cybersource tests

### Added
- [ENGINEERS-1138] - In Cypress, Added optional Order Suffix to reference number testcase

## [1.13.9] - 2023-02-03

### Added
- Added ListOrders policy

## [1.13.8] - 2023-02-03

### Changed
- installmentInformation.planType always 1

## [1.13.7] - 2023-02-02

### Fixed
- Add optional Order Suffix to refernce number when searching Cyberouse transactions

### Changed
- [ENGINEERS-1109] - Fix payer auth refund tests

### Changed
- [ENGINEERS-1039] - Enabled payer auth testcase

## [1.13.6] - 2023-01-26

### Changed
- Retrieve Transaction Route made private

## [1.13.5] - 2023-01-26

### Changed
- Removed threeDSServerTransactionId & acsTransactionId from Payer Auth requests
- Removed referenceDataNumber from Item
- For Banorte, installmentPlan Type is 2 for AMEX, 1 for everything else

### Added
-  Verify relatedTransactions property from refund transaction 

## [1.13.4] - 2023-01-19

### Fixed
- Only add reconciliationId to refunds for Banote
- Verify Status is PENDING for Captures and Refunds

## [1.13.3] - 2023-01-19

### Changed
- Log full capture request payload for Ecuador
- Include stack trace in capture error log

### Changed
- [ENGINEERS-1078] - Now, cybersource returns relatedTransactions only after refund
  So, array length would be 1 instead of 2.

## [1.13.2] - 2023-01-12

### Changed
- Do not set administrativeArea for Costa Rica and El Salvador

## [1.13.1] - 2023-01-12

### Fixed
- Empty authorization id on capture

## [1.13.0] - 2023-01-10

### Changed
- Do not set administrativeArea for Bolivia, Guatemala, Puerto Rico and the Dominican Republic

## [1.12.0] - 2023-01-10

### Added
Added option to set status for the "AUTHORIZED_RISK_DECLINED" case

## [1.11.0] - 2023-01-06

### Added
- (CYBRSOURCE-62) Custom Capture payload for Ecuador

### Changed
- [ENGINEERS-1029] - Updated tax values in cypress tests 

## [1.10.4] - 2023-01-04

### Changed
- Payer Auth performance updates

## [1.10.3] - 2022-12-22

### Fixed
- Verify that Capture and Refund amounts are greater than zero

## [1.10.2] - 2022-12-20

### Fixed
- Tax Amount field default

## [1.10.1] - 2022-12-20

### Fixed
- Tax Amount field formatting

## [1.10.0] - 2022-12-19

### Added
- Discover as supported payment method

## [1.9.2] - 2022-12-15

### Changed
- Payer Auth updates

## [1.9.1] - 2022-12-08

### Changed
- [ENGINEERS-969] - Skip payer auth active testcase 

### Added
- Payer Auth testcase in cypress

### Fixed
- Payer Auth Setting Check error

## [1.9.0] - 2022-12-06

### Changed
- Payer Auth Validation is now a separate call
- Round shipping tax to two decimal places

### Changed
- Move all settlements related testcases to settlements.spec.js
- Updated cy-runnner strategy, set maxJobs from 3 to 2, increase basicTests hardRetries from 1 to 2

## [1.8.7] - 2022-11-25

### Fixed

- Rounding for EC tax fields

## [1.8.6] - 2022-11-23

### Fixed

- Decimal places to two digits for EC tax fields

## [1.8.5] - 2022-11-22

### Changed
- Changed order of operations when calculating tax detail amount

## [1.8.4] - 2022-11-21

### Changed
- Payer Auth updates

### Fixed
- (CYBRSOURCE-53 & CYBRSOURCE-54) If there is an error processing payment, attempt to retrieve the transaction from Cybersource

### Added
- Add reconciliationId to refunds

## [1.8.3] - 2022-11-10

### Added
- (CYBRSOURCE-55) Ecuador payload customizations: add shipping tax as a line item

## [1.8.2] - 2022-11-03

### Changed
- Fix critical security vulnerability issue bumping minimist to 1.2.7

## [1.8.1] - 2022-11-01

### Changed
- vm2 package updated to 3.9.11 due a critical security vulnerability

## [1.8.0] - 2022-10-20

### Added
- In cypress tests, Select Credit Card then make payment 

### Fixed
- Region setting bug
### Added
- (CYBRSOURCE-40) Functions to support Payer Auth

## [1.7.3] - 2022-10-12

### Fixed
- Ecuador payload customizations

## [1.7.2] - 2022-10-12

### Changed
- Fix on new Cypress reserve/release orderForm feature
### Fixed
- Fix order id lookup
### Changed
- Functions to support Payer Auth

## [1.7.1] - 2022-10-07

### Fixed
- Ecuador payload customizations

## [1.7.0] - 2022-10-07

### Added
- Added custom parser for Panama region codes

## [1.6.2] - 2022-10-06

### Changed
- Functions to support Payer Auth
- On CreatePayment, look up order id from ref
- GitHub reusable workflow and cy-runner updated to version 2

## [1.6.1] - 2022-09-29

### Added
- Added call to local bin lookup
### Changed
- Changed installments behavior

## [1.6.0] - 2022-09-21

### Changed
- (CYBRSOURCE-48) Use order number as reference for cancel transactions
### Added
- Ecuador payload customizations

## [1.5.0] - 2022-09-20

### Added
- (CYBRSOURCE-46) Optional order reference suffix
- Functions to support Payer Auth

## [1.4.6] - 2022-09-02

### Fixed
- Updated ISO region code translation

## [1.4.5] - 2022-09-01

### Fixed
- Fixed reconciliationId error

## [1.4.4] - 2022-08-31

### Added
- Installments Plan Type
- Custom NSU

## [1.4.3] - 2022-08-29

### Fixed
- Fixed region codes

## [1.4.2] - 2022-08-17

### Added
- Ecuador region codes
- Panama region codes

### Fixed
- (CYBRSOURCE-43) Added await to order Config changes

## [1.4.1] - 2022-08-17

### Fixed
- (CYBRSOURCE-44) Fixed region code lookup
- (CYBRSOURCE-44) For shipping address, only use Receiver Name if more than two characters, otherwise use payer name.

### Added
- Colombia region codes
- Peru region codes
- Mexico region codes

## [1.4.0] - 2022-08-08

### Added
- Added more merchant defined fields
- Added Payer Auth Services
- Send neighborhood as district

## [1.3.4] - 2022-08-05

### Added
- GraphQL mutation security.
- Logging optimization.

## [1.3.3] - 2022-08-05

### Changed

- If city is null, use neighborhood value for locality

## [1.3.2] - 2022-07-11

### Changed
- Updated ReadMe

## [1.3.1] - 2022-06-22

- (CYBRSOURCE-33) Convert Chile region name to iso code

## [1.3.0] - 2022-06-15

### Fixed
- If capture fails check for previous capture
- Remove region code from administrative area

### Added
- (CYBRSOURCE-32) Allow setting different credentials on Gateway

## [1.2.0] - 2022-06-10

### Added
- Added call to get ContextData
- Added docs builder

### Changed
- Get order from checkout instead of oms

## [1.1.44] - 2022-05-31

### Changed
- If tax nexus is not specified, calculate tax.

## [1.1.43] - 2022-05-27

### Added
- Shipping product code

## [1.1.42] - 2022-05-25

### Changed
- Allow blank lines in MDD
- Changed shipping product code

## [1.1.41] - 2022-05-19

### Added

- Added free bin lookup service as fallback

## [1.1.40] - 2022-05-18

### Changed
- Changed custom payload for eGlobal & BBVA

## [1.1.39] - 2022-05-17

### Fixed
- Auth and Capture fix

## [1.1.38] - 2022-05-11

### Fixed
- Custom MDD

## [1.1.37] - 2022-05-10
- Auth & Bill
## [1.1.36] - 2022-05-10

### Fixed
- Capture error

## [1.1.35] - 2022-05-09

### Changed

- Custom payment options

## [1.1.34] - 2022-05-09

### Added
- (CYBRSOURCE-27) Added option for auth & capture
### Fixed
- Shipping data

## [1.1.33] - 2022-05-06

- (CYBRSOURCE-25) Do not pass fraud score because status must be controled by Decision Manager
- (CYBRSOURCE-27) Configure Cybersource gateway to use auth/capture
- (CYBRSOURCE-28) Add Marketing Data to Merchant Defined Fields

## [1.1.32] - 2022-04-14

### Fixed

- Custom Apps to MDD

## [1.1.31] - 2022-04-08

### Fixed

- taxes

## [1.1.30] - 2022-04-05

### Added

- Custom Apps data

## [1.1.29] - 2022-04-01

### Added

- Solution Id

## [1.1.28] - 2022-03-31

### Added

- Debug Device Fingerprint

## [1.1.27] - 2022-03-29

### Fixed

- DM notifications

## [1.1.26] - 2022-03-29

## [1.1.25] - 2022-03-28

## [1.1.24] - 2022-03-25

## [1.1.23] - 2022-03-25

## [1.1.22] - 2022-03-25

### Added

- BIN lookup

## [1.1.21] - 2022-03-25

### Added

- error logging

## [1.1.20] - 2022-03-24

## [1.1.19] - 2022-03-24

### Changed

- Check fraud status when setting auth result

## [1.1.18] - 2022-03-24

### Changed

- Use OrderId as reference number

## [1.1.17] - 2022-03-23

### Changed

- Look up sequence number

## [1.1.16] - 2022-03-23

## [1.1.15] - 2022-03-23

### Changed

- Changed OrderId to Reference for payment to be consistent with Fraud 

## [1.1.14] - 2022-03-23

### Changed

- Antifraud changes

## [1.1.13] - 2022-03-22

### Changed

- Antifraud changes

## [1.1.12] - 2022-03-22

### Changed

- Testing, Debug

## [1.1.11] - 2022-03-21

### Changed

- Testing, Debug

## [1.1.10] - 2022-03-18

### Changed

- Testing, Debug

## [1.1.9] - 2022-03-15

## [1.1.8] - 2022-03-14

### Changed

- Callback url
- Merchant values
- Item prices

## [1.1.7] - 2022-03-14

## [1.1.6] - 2022-03-14

## [1.1.5] - 2022-03-11

## [1.1.4] - 2022-03-11

## [1.1.3] - 2022-03-11

## [1.1.2] - 2022-03-11

## [1.1.1] - 2022-03-11

### Changed

- Proxy testing

## [1.1.0] - 2022-03-10

### Changed

- Added parameter for base64 key when generating signature

## [1.0.3] - 2022-02-18

## [1.0.2] - 2022-02-16

## [1.0.1] - 2022-02-10

### Changed

- Set proxy response to Base64

## [1.0.0] - 2022-01-25

### Changed

- Split app

## [0.2.1] - 2021-10-18

## [0.2.0] - 2021-10-08

### Added

- Conversion Report
- Decision Manager

## [0.1.0] - 2021-09-24
### Added

- Admin Page Added

## [0.0.3] - 2021-09-08


## [0.0.2] - 2021-07-07

## [0.0.1] - 2021-06-02

### Added

- Initial version
