📢 Use this project, [contribute](https://github.com/vtex-apps/cybersource) to it or open issues to help evolve it using [Store Discussion](https://github.com/vtex-apps/store-discussion).

<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->

[![All Contributors](https://img.shields.io/badge/all_contributors-0-orange.svg?style=flat-square)](#contributors-)

<!-- ALL-CONTRIBUTORS-BADGE:END -->

# Cybersource IO

This app uses Cybersource REST API to process Payments, Risk Management, and Taxes

## Configuration

1. [Install](https://developers.vtex.com/vtex-developer-docs/docs/vtex-io-documentation-installing-an-app) `vtex.cybersource` & `vtex.cybersource-ui` in the desired account.

2. In Cybersource EBC, generate authentication keys.
	- Payment Configuration -> Key Management -> Generate Key
	- Choose `REST - Shared Secret` and Generate Key

3. In VTEX Admin, select Cybersource App and enter key values.

4. Transactions -> Payments -> Settings
	- Select Gateway Affiliations and click the green plus
	- Select Cybersource (Ensure the url is `/admin/pci-gateway/#/affiliations/vtex-cybersource-v1/`)

5. Payment Conditions
	- Add New Payment using Gateway

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<!-- markdownlint-enable -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!
