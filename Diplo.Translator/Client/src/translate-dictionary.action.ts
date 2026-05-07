import { UMB_ACTION_EVENT_CONTEXT, type UmbActionEventContext } from "@umbraco-cms/backoffice/action";
import { UMB_COLLECTION_CONTEXT, type UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_DICTIONARY_ROOT_ENTITY_TYPE } from "@umbraco-cms/backoffice/dictionary";
import { UmbEntityActionBase, type UmbEntityActionArgs } from "@umbraco-cms/backoffice/entity-action";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { DIPLO_TRANSLATOR_MODAL } from "./translate-dictionary-modal.token";

type RefreshableCollectionElement = HTMLElement & {
  _api?: { requestCollection?: () => Promise<void> | void };
};

export class DiploTranslateDictionaryAction extends UmbEntityActionBase<never> {
  #actionEventContext?: UmbActionEventContext;
  #collectionContext?: UmbDefaultCollectionContext;

  constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
    super(host, args);

    this.consumeContext(UMB_ACTION_EVENT_CONTEXT, (context) => {
      this.#actionEventContext = context;
    });

    this.consumeContext(UMB_COLLECTION_CONTEXT, (context) => {
      this.#collectionContext = context;
    });
  }

  async execute(): Promise<void> {
    await umbOpenModal(this, DIPLO_TRANSLATOR_MODAL, {
      data: {
        onComplete: () => this.#refreshDictionaryViews()
      }
    }).catch(() => undefined);
  }

  async #refreshDictionaryViews(): Promise<void> {
    this.#actionEventContext?.dispatchEvent(new UmbRequestReloadChildrenOfEntityEvent({
      unique: null,
      entityType: UMB_DICTIONARY_ROOT_ENTITY_TYPE
    }));

    await this.#collectionContext?.requestCollection();
    await this.#refreshDictionaryOverviewCollectionElement();
  }

  async #refreshDictionaryOverviewCollectionElement(): Promise<void> {
    const dashboard = this.#querySelectorDeep(document, "umb-dictionary-collection-dashboard");
    const collectionRoot = dashboard?.shadowRoot ?? dashboard;
    const collection = collectionRoot
      ? this.#querySelectorDeep(collectionRoot, "umb-collection") as RefreshableCollectionElement | null
      : null;

    await collection?._api?.requestCollection?.();
  }

  #querySelectorDeep(root: ParentNode, selector: string): Element | null {
    const directMatch = root.querySelector(selector);

    if (directMatch) {
      return directMatch;
    }

    for (const element of root.querySelectorAll("*")) {
      if (!element.shadowRoot) {
        continue;
      }

      const shadowMatch = this.#querySelectorDeep(element.shadowRoot, selector);

      if (shadowMatch) {
        return shadowMatch;
      }
    }

    return null;
  }
}

export default DiploTranslateDictionaryAction;
