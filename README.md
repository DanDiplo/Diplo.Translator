# Diplo Translator for Umbraco

Diplo Translator is an Umbraco 17 package for translating Umbraco Dictionary items from the backoffice.

This version targets **Umbraco 17** and **.NET 10**. Translation is provided through the free [Umbraco.AI](https://docs.umbraco.com/ai-in-umbraco), so the package no longer talks directly to Microsoft Translator or stores Microsoft Translator API credentials.
## Features

- Adds a **Translate** action to the Dictionary tree.
- Translates missing Dictionary values from a selected source language.
- Optional overwrite mode for replacing existing translated values.
- Uses [Umbraco.AI](https://marketplace.umbraco.com/package/umbraco.ai) chat profiles, including the host site's default profile.
- Runs translations as a background job with progress polling in the backoffice modal.
- Continues processing if an individual Dictionary item fails and reports per-item errors.

## Requirements

- Umbraco 17
- .NET 10
- An [Umbraco.AI](https://marketplace.umbraco.com/category/artificial-intelligence?supportsUmbracoVersionNumber=17.3&maintainedBy=hq) provider installed and configured in the host site, for example [Umbraco.AI.OpenAI](https://marketplace.umbraco.com/package/umbraco.ai.openai)
- At least two languages configured in Umbraco

## Installation

Install the package into your Umbraco 17 site:

```powershell
dotnet add package Diplo.Translator
```

The package ships its backoffice assets under:

```text
/App_Plugins/DiploTranslator/
```

## Umbraco.AI Setup

Configure [Umbraco.AI](https://marketplace.umbraco.com/package/umbraco.ai) in the host site as normal with at least one provider.

Diplo Translator uses `IAIChatService`, so it will use the configured chat provider and chat profile from Umbraco.AI.

By default, the package allows Umbraco.AI to resolve the default chat profile.

If you want Diplo Translator to use a specific chat profile, add this to `appsettings.json`:

```json
{
  "DiploTranslator": {
    "ChatProfileAlias": "your-chat-profile-alias"
  }
}
```

Leave `ChatProfileAlias` empty or remove it to use the default Umbraco.AI chat profile.

## Suggested AI Profile

Use a model/profile that is good at short, literal translation. For small Dictionary values, a compact model is usually enough. For instance, if using [Open.AI](https://openai.com/) then use `gpt-4.1-nano` which is fast and very efficient.

A useful system prompt for the `Umbraco.AI` profile is:

```text
You are a strict translation engine. Return only the translated text. Do not add markdown, commentary, labels, surrounding quotes, or explanations. Preserve placeholders, dictionary tokens, URLs, HTML tags, whitespace-sensitive fragments, and formatting markers.
```

The package also sends its own translation instruction with each request, so a profile-level system prompt is optional. Adding one is still helpful if the same chat profile is dedicated to translation.

## Usage

Open the Umbraco backoffice and go to the Dictionary tree.

Use the **Translate** entity action on the Dictionary root or a Dictionary item. The action opens a modal where you can:

- choose the source language
- choose whether to overwrite existing target values
- start the translation job
- watch progress and item errors

The source language defaults to Umbraco's default language where possible.

When the job runs, the package:

- collects the selected Dictionary item and its descendants
- reads the value for the selected source language
- translates missing target-language values
- skips existing target-language values unless overwrite is enabled
- saves values through Umbraco's localization services

## Example

If your site has English, French, and German, and English is your source language:

- fill in the English Dictionary values
- run **Translate**
- the package translates missing French and German values

If **Overwrite existing values** is disabled, existing French and German values are left alone.

## Development

The solution contains:

```text
Diplo.Translator/
  Client/                         Vite, Lit, TypeScript backoffice UI
  wwwroot/App_Plugins/DiploTranslator/  generated backoffice assets
  Controllers/                    Management API endpoints
  Services/                       Dictionary traversal, jobs, Umbraco.AI translation

Diplo.Translator.Testsite/        Umbraco 17 test site
```

Build the package and test site:

```powershell
dotnet build Diplo.Translator.Testsite\Diplo.Translator.Testsite.csproj /p:UseAppHost=false
```

Build the client directly:

```powershell
cd Diplo.Translator\Client
npm ci
npm run type-check
npm run build
```

The generated client bundle is ignored by git and rebuilt during package builds.

## Management API

The package exposes a Management API under:

```text
/umbraco/management/api/v1/diplo-translator
```

Endpoints:

- `GET /languages`
- `GET /settings`
- `POST /jobs`
- `GET /jobs/{jobId}`
- `DELETE /jobs/{jobId}`

The backoffice modal uses these endpoints to load languages, start translation jobs, poll progress, and cancel a running job.

## Notes

- This v17 version replaces the old AngularJS backoffice integration with a Lit-based extension.
- SignalR is no longer used; progress is reported through HTTP polling.
- Microsoft Translator configuration is no longer used.
- `ILocalizationService` is still used for Dictionary persistence in Umbraco 17, but Umbraco marks it obsolete for removal in Umbraco 18.

## Links

- NuGet: https://www.nuget.org/packages/Diplo.Translator/
- Source: https://github.com/DanDiplo/Diplo.Translator
- Umbraco.AI docs: https://docs.umbraco.com/ai-in-umbraco

## Credits

Created by Dan "Diplo" Booth.
