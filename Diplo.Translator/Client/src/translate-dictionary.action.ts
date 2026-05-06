import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { DIPLO_TRANSLATOR_MODAL } from "./translate-dictionary-modal.token";

export class DiploTranslateDictionaryAction extends UmbEntityActionBase<never> {
  async execute(): Promise<void> {
    await umbOpenModal(this, DIPLO_TRANSLATOR_MODAL);
  }
}

export default DiploTranslateDictionaryAction;
