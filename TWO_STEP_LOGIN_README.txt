Two-step sign-in (introduced V10.24, current release V10.27)

Registered by the Super User only. Page: TwoStepSetup.aspx (menu link "Two-Step", visible to Super User).
The Super User sets it up for their own account and for any other user (enter the username, add the
setup key to that person's authenticator app, confirm with the 6-digit code, then hand over the backup codes).
Org admins and other users cannot enrol or change it themselves. Users who have it enabled are asked
for a code at sign-in; everyone else signs in as before.
Switch off for everyone without redeploying: Web.config appSettings TwoStepEnabled = false.
Database: four columns are added to Users automatically (TotpSecret, TotpEnabled, TotpBackupCodes, TotpLastStep).
If they cannot be created, normal login keeps working.
Lost phone: Super User uses "Reset (Turn Off) For This User", then registers again; or the user enters a backup code.
Limits: 5 wrong codes per sign-in attempt, 10 per 15 minutes per user.
Test on a copy first, with a second Super User session kept signed in as a fallback.

QR code: the setup page shows a QR code (Content/qr-mini.js, generated in the browser, nothing sent to any outside service).
The text setup key and "Open in authenticator app" link remain as fallbacks. Do not screenshot or share the QR code; it is the user's secret.
A Super User can no longer reset their own account with the "Other users" box (use Turn Off with a code instead).
