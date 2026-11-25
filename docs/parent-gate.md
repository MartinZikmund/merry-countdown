# The grown-ups' question

Purchases, links that leave the app and the grown-ups' corner sit behind `IParentGateService.PassAsync`: a
multiplication question written in words, which a parent answers in a second and a small child can't.

## It is not asked every time

- A right answer is remembered for 15 minutes (`ParentGatePass.Cooldown`), for the whole app, until it is closed.
- **Keep this corner unlocked**, at the end of the grown-ups' corner, turns the question off on that device
  (`FamilySettings.KeepGrownUpsUnlocked`). It is off by default.

With the question off, anyone holding the device can reach the purchase and open links. The stores' rules for apps
aimed at children expect a gate in front of both, so think before shipping it on by default or removing the gate.

## The pieces

- `ParentGate` (Core) makes the question and checks the answer. `ParentGatePass` (Core) decides whether to ask;
  `ParentGateTests` covers both.
- `ParentGateService` (head) shows the dialog, and skips it while `ParentGatePass.IsOpen`.
