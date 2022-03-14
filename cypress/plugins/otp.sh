#!/bin/bash

[ "x$TWILIO_USER" == "x" ] && echo "You must set TWILIO_USER env" >&2 && exit 1
[ "x$TWILIO_TOKEN" == "x" ] && echo "You must set TWILIO_TOKEN env" >&2 && exit 2

A=0; B=1; sleep 5
while [[ $A -ne $B ]]; do

  A=$(curl "https://api.twilio.com/2010-04-01/Accounts/$TWILIO_USER/Messages.json" \
      -u $TWILIO_USER:$TWILIO_TOKEN 2> /dev/null | jq .messages[0].body | \
      sed -e 's/ //g' -e 's/"//g' | cut -d '\' -f1)

  sleep 2

  B=$(curl "https://api.twilio.com/2010-04-01/Accounts/$TWILIO_USER/Messages.json" \
      -u $TWILIO_USER:$TWILIO_TOKEN 2> /dev/null | jq .messages[0].body | \
      sed -e 's/ //g' -e 's/"//g' | cut -d '\' -f1)

done
[ $A == "null" ] && echo "Error getting OTP code from Twilio" >&2 && exit 3
echo $A