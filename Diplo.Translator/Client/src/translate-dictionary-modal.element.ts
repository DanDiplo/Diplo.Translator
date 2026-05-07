import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT, type UmbNotificationContext } from "@umbraco-cms/backoffice/notification";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import {
  cancelJob,
  getJob,
  getLanguages,
  getSettings,
  startJob,
  type TranslationJob,
  type TranslatorLanguage,
  type TranslatorSettings
} from "./api";
import type { DiploTranslatorModalData } from "./translate-dictionary-modal.token";

const COMPLETE_STATUSES = ["Completed", "CompletedWithErrors", "Failed", "Cancelled"];
const SUCCESS_STATUSES = ["Completed", "CompletedWithErrors"];

@customElement("diplo-translate-dictionary-modal")
export class DiploTranslateDictionaryModalElement extends UmbModalBaseElement<DiploTranslatorModalData, never> {
  #notificationContext?: UmbNotificationContext;
  #pollTimer?: number;
  #jobId?: string;
  #hasRefreshedDictionaryView = false;

  @state()
  private _languages: TranslatorLanguage[] = [];

  @state()
  private _settings?: TranslatorSettings;

  @state()
  private _fromCulture = "";

  @state()
  private _overwrite = false;

  @state()
  private _isLoading = true;

  @state()
  private _isStarting = false;

  @state()
  private _job?: TranslationJob;

  @state()
  private _error?: string;

  constructor() {
    super();
    this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
      this.#notificationContext = context;
    });
  }

  override connectedCallback(): void {
    super.connectedCallback();
    void this.#load();
  }

  override disconnectedCallback(): void {
    window.clearTimeout(this.#pollTimer);
    super.disconnectedCallback();
  }

  async #load(): Promise<void> {
    this._isLoading = true;
    this._error = undefined;

    try {
      const [languages, settings] = await Promise.all([getLanguages(), getSettings()]);
      this._languages = languages;
      this._settings = settings;
      this._fromCulture = languages.find((language) => language.isDefault)?.isoCode ?? languages[0]?.isoCode ?? "";
    } catch (error) {
      this._error = this.#errorMessage(error);
    } finally {
      this._isLoading = false;
    }
  }

  async #start(): Promise<void> {
    if (!this._fromCulture) {
      return;
    }

    this._isStarting = true;
    this._error = undefined;

    try {
      this.#jobId = await startJob(this._fromCulture, this._overwrite);
      await this.#poll();
    } catch (error) {
      this._error = this.#errorMessage(error);
    } finally {
      this._isStarting = false;
    }
  }

  async #poll(): Promise<void> {
    if (!this.#jobId) {
      return;
    }

    try {
      this._job = await getJob(this.#jobId);

      if (COMPLETE_STATUSES.includes(this._job.status)) {
        this.#notifyComplete(this._job);
        void this.#refreshDictionaryView(this._job);
        return;
      }

      this.#pollTimer = window.setTimeout(() => void this.#poll(), 1000);
    } catch (error) {
      this._error = this.#errorMessage(error);
    }
  }

  async #cancel(): Promise<void> {
    if (!this.#jobId) {
      this.modalContext?.reject();
      return;
    }

    try {
      await cancelJob(this.#jobId);
      await this.#poll();
    } catch (error) {
      this._error = this.#errorMessage(error);
    }
  }

  #notifyComplete(job: TranslationJob): void {
    const progress = job.progress;
    const message = `Translated ${progress.translationCount} values with ${progress.errorCount} errors.`;
    const type = job.status === "Completed" ? "positive" : job.status === "CompletedWithErrors" ? "warning" : "danger";

    this.#notificationContext?.peek(type, {
      data: {
        headline: "Dictionary translation complete",
        message
      }
    });
  }

  async #refreshDictionaryView(job: TranslationJob): Promise<void> {
    if (this.#hasRefreshedDictionaryView || !SUCCESS_STATUSES.includes(job.status)) {
      return;
    }

    this.#hasRefreshedDictionaryView = true;
    await this.data?.onComplete?.();
  }

  #errorMessage(error: unknown): string {
    if (error instanceof Error) {
      return error.message;
    }

    return "The translator could not complete the request.";
  }

  #renderBody() {
    if (this._isLoading) {
      return html`<uui-loader></uui-loader>`;
    }

    return html`
      ${this._error ? html`<uui-box class="error">${this._error}</uui-box>` : null}
      ${this.#renderSettingsNotice()}
      ${this.#renderForm()}
      ${this._job ? this.#renderProgress(this._job) : null}
    `;
  }

  #renderSettingsNotice() {
    if (!this._settings || this._settings.hasConfiguredChatProfileAlias) {
      return null;
    }

    return html`<uui-box class="notice">Using the default Umbraco.AI chat profile.</uui-box>`;
  }

  #renderForm() {
    const disabled = this._isStarting || (this._job !== undefined && !COMPLETE_STATUSES.includes(this._job.status));

    return html`
      <uui-form-layout-item>
        <uui-label for="fromCulture" slot="label">Translate from</uui-label>
        <uui-select
          id="fromCulture"
          .options=${this._languages.map((language) => ({
            name: `${language.displayName} (${language.isoCode})`,
            value: language.isoCode,
            selected: language.isoCode === this._fromCulture
          }))}
          ?disabled=${disabled}
          @change=${(event: Event) => {
            this._fromCulture = (event.target as HTMLSelectElement).value;
          }}
        ></uui-select>
      </uui-form-layout-item>

      <uui-form-layout-item>
        <uui-label for="overwrite" slot="label">Overwrite existing values</uui-label>
        <uui-toggle
          id="overwrite"
          .checked=${this._overwrite}
          ?disabled=${disabled}
          @change=${(event: Event) => {
            this._overwrite = (event.target as HTMLInputElement).checked;
          }}
        ></uui-toggle>
      </uui-form-layout-item>
    `;
  }

  #renderProgress(job: TranslationJob) {
    const progress = job.progress;
    const percentage = progress.dictionaryCount > 0
      ? Math.round((progress.processedItemCount / progress.dictionaryCount) * 100)
      : 0;

    return html`
      <section class="progress">
        <uui-progress-bar progress=${percentage}></uui-progress-bar>
        <dl>
          <div><dt>Status</dt><dd>${job.statusMessage}</dd></div>
          <div><dt>Items</dt><dd>${progress.processedItemCount} / ${progress.dictionaryCount}</dd></div>
          <div><dt>Translations</dt><dd>${progress.translationCount} / ${progress.attemptedTranslationCount}</dd></div>
          <div><dt>Skipped</dt><dd>${progress.skippedCount}</dd></div>
          <div><dt>Errors</dt><dd>${progress.errorCount}</dd></div>
        </dl>
        ${progress.currentItem ? html`<p class="current">Current: ${progress.currentItem}</p>` : null}
        ${job.errors.length ? html`<ul class="errors">${job.errors.map((error) => html`<li>${error}</li>`)}</ul>` : null}
      </section>
    `;
  }

  override render() {
    const hasRunningJob = this._job !== undefined && !COMPLETE_STATUSES.includes(this._job.status);
    const hasSuccessfulJob = this._job !== undefined && SUCCESS_STATUSES.includes(this._job.status);

    return html`
      <umb-body-layout headline="Translate dictionary">
        ${this.#renderBody()}
        <uui-button slot="actions" type="button" label="Close" look="secondary" @click=${() => this.modalContext?.reject()}></uui-button>
        ${hasRunningJob
          ? html`<uui-button slot="actions" type="button" label="Cancel" look="secondary" color="danger" @click=${this.#cancel}></uui-button>`
          : hasSuccessfulJob
            ? null
            : html`<uui-button
              slot="actions"
              type="button"
              label="Translate"
              look="primary"
              color="positive"
              ?disabled=${this._isLoading || this._isStarting || !this._fromCulture}
              @click=${this.#start}
            ></uui-button>`}
      </umb-body-layout>
    `;
  }

  static override styles = [
    UmbTextStyles,
    css`
      :host {
        display: block;
      }

      uui-form-layout-item {
        margin-bottom: var(--uui-size-layout-1);
      }

      .notice,
      .error {
        margin-bottom: var(--uui-size-layout-1);
      }

      .error {
        color: var(--uui-color-danger);
      }

      .progress {
        margin-top: var(--uui-size-layout-1);
      }

      dl {
        display: grid;
        gap: var(--uui-size-2);
        margin: var(--uui-size-layout-1) 0;
      }

      dl div {
        display: flex;
        justify-content: space-between;
        gap: var(--uui-size-layout-1);
      }

      dt {
        font-weight: 700;
      }

      dd {
        margin: 0;
        text-align: right;
      }

      .current {
        color: var(--uui-color-text-alt);
      }

      .errors {
        color: var(--uui-color-danger);
        padding-left: var(--uui-size-layout-1);
      }
    `
  ];
}

export default DiploTranslateDictionaryModalElement;

declare global {
  interface HTMLElementTagNameMap {
    "diplo-translate-dictionary-modal": DiploTranslateDictionaryModalElement;
  }
}
