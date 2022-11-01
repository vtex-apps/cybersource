# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- (CYBRSOURCE-55) Ecuador payload customizations: add shipping tax as a line item

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
