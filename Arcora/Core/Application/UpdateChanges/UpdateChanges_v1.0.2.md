# arcora 1.0.2

A more reliable Remote Monitoring.

---

## Bug fixes

- **Remote Monitoring:** when arcora on the PC was busy for a while, your phone showed *"arcora did not answer"*, but the monitoring could still start afterwards and black out all screens without your phone knowing about it. Now the monitoring either starts and your phone shows it, or it doesn't start at all and your phone tells you that the PC was busy.
- **Remote Monitoring:** blacking out the screens no longer delays the answer to your phone.
- **Remote Monitoring:** while arcora couldn't be reached (e.g. not started yet), your phone asked you to scan the QR code again, although it was still paired. It now says that arcora can't be reached and tries again by itself.
- **Remote Monitoring:** your phone notices a lost connection to arcora faster.

## New features

- **Settings → Advanced:** a new page for options that help track down problems. **Extended remote monitoring logging** writes every request of your phone to the log, and how long a slow start or stop of the monitoring took. Leave it off unless you are looking into a problem, the log grows quickly.

## Improvements

- **Settings → Monitoring:** the separate line with the port of the Remote Monitoring is gone, the address is shown next to the QR code.
