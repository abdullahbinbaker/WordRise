# Integrations

Adapters for services outside Wordrise, one project per provider:

- `Wordrise.Integrations.Claude`: lesson authoring and review (AI).
- `Wordrise.Integrations.AzureSpeech`: American English voices, later pronunciation scoring.
- `Wordrise.Integrations.Email`: account confirmation and password reset.

Rules:

- No business logic here: an integration only calls the external API and maps its answers.
- A module never calls an external API directly. It defines the interface it needs and an
  integration implements it, so a provider can be replaced without touching the module.
- Keys and endpoints come from configuration, never from code.
