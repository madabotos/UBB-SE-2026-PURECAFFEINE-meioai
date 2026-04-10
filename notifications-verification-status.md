# Notifications Verification Status

Current branch notification coverage and manual verification status.

## 1) Booking Unavailable
- Trigger: request approved and overlapping requests are auto-cancelled (`ApproveRequest`)
- Recipient: overlapping request renter(s)
- Type: `Informational`
- Source: `Property_and_Management/src/Service/RequestService.cs` (ApproveRequest, around line 186)
- Status: **Reviewed, not fully verified yet**
- Notes: cannot be fully verified in current setup; needs at least 3 users / a test scenario that reliably creates overlapping requests and then approves one.

## 2) Rental request declined
- Trigger: owner denies request (`DenyRequest`)
- Recipient: renter
- Type: `Informational`
- Source: `Property_and_Management/src/Service/RequestService.cs` (DenyRequest, around line 262)
- Status: **Reviewed, not verified yet**
- Notes: cannot be verified from current UI because deny action is missing in `Others' Requests` page (owner should be able to deny there).

## 3) Rental request cancelled
- Trigger: game deleted and message to the existing requests on that game(`OnGameDeactivated`)
- Recipient: renter(s) with pending request
- Type: `Informational`
- Source: `Property_and_Management/src/Service/RequestService.cs` (OnGameDeactivated, around line 298)
- Status: **Not yet verified**

## 4) Game Offer Received
- Trigger: owner offers game (`OfferGame`)
- Recipient: renter
- Type: `OfferReceived` (actionable, includes `RelatedRequestId`)
- Source: `Property_and_Management/src/Service/RequestService.cs` (OfferGame, around line 391)
- Status: **Not yet verified**

## 5) Offer Accepted
- Trigger: renter approves offer (`ApproveOffer`)
- Recipient: owner/offering user
- Type: `OfferResult`
- Source: `Property_and_Management/src/Service/RequestService.cs` (ApproveOffer, around line 486)
- Status: **Not yet verified**

## 6) Rental Confirmed
- Trigger: renter approves offer (`ApproveOffer`)
- Recipient: renter
- Type: `OfferResult`
- Source: `Property_and_Management/src/Service/RequestService.cs` (ApproveOffer, around line 498)
- Status: **Not yet verified**

## 7) Offer Denied
- Trigger: renter denies offer (`DenyOffer`)
- Recipient: owner/offering user
- Type: `OfferResult`
- Source: `Property_and_Management/src/Service/RequestService.cs` (DenyOffer, around line 541)
- Status: **Not yet verified**

## 8) Offer Declined
- Trigger: renter denies offer (`DenyOffer`)
- Recipient: renter (confirmation)
- Type: `OfferResult`
- Source: `Property_and_Management/src/Service/RequestService.cs` (DenyOffer, around line 554)
- Status: **Not yet verified**

## 9) Upcoming Rental Reminder
- Trigger: scheduled 24h before rental start
- Recipient: renter and owner
- Type: `Informational` (default)
- Source: `Property_and_Management/src/Service/NotificationService.cs` (ScheduleUpcomingRentalReminder, around line 150)
- Status: **Not yet verified**
