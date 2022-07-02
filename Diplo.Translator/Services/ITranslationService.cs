using Diplo.Translator.Models;
using System.Threading.Tasks;

namespace Diplo.Translator.Services
{
    public interface ITranslationService
    {
        /// <summary>
        /// Translates the given text
        /// </summary>
        /// <param name="text">The plain text to translate</param>
        /// <param name="to">The language code to translate to</param>
        /// <param name="from">The optional language being translated from. If left blank uses auto-detection.</param>
        /// <returns>A translation response</returns>
        Task<TranslationResponse> TranslateAsync(string text, string to, string from = null);
    }
}