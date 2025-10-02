using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ApiRefeicoes.Constants;

namespace ApiRefeicoes.Services
{
    public class FaceApiService
    {
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _personGroupId;
        private readonly ILogger<FaceApiService> _logger;

        public FaceApiService(IConfiguration configuration, ILogger<FaceApiService> logger)
        {
            _apiKey = configuration["AzureFaceApi:ApiKey"];
            _endpoint = configuration["AzureFaceApi:Endpoint"];
            _personGroupId = configuration["AzureFaceApi:PersonGroupId"];
            _logger = logger;
        }

        private IFaceClient GetClient()
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(_apiKey)) { Endpoint = _endpoint };
        }

        public async Task EnsurePersonGroupExistsAsync()
        {
            var client = GetClient();
            try
            {
                await client.PersonGroup.GetAsync(_personGroupId, returnRecognitionModel: true);
                _logger.LogInformation("[{Timestamp}] Grupo '{PersonGroupId}' já existe.", DateTime.UtcNow, _personGroupId);
            }
            catch (APIErrorException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[{Timestamp}] Grupo '{PersonGroupId}' não encontrado. Criando novo com modelo {RecognitionModel}.", DateTime.UtcNow, _personGroupId, FaceApiModels.RecognitionModel);

                await client.PersonGroup.CreateAsync(
                    personGroupId: _personGroupId,
                    name: "Grupo de Colaboradores do Refeitório",
                    userData: "Grupo criado automaticamente",
                    recognitionModel: FaceApiModels.RecognitionModel
                );

                _logger.LogInformation("[{Timestamp}] Grupo '{PersonGroupId}' criado com sucesso.", DateTime.UtcNow, _personGroupId);
            }
        }

        public async Task<(Guid? faceId, string message)> DetectFaceWithFeedback(Stream imageStream)
        {
            var client = GetClient();
            try
            {
                imageStream.Position = 0;
                var faces = await client.Face.DetectWithStreamAsync(imageStream, recognitionModel: FaceApiModels.RecognitionModel, detectionModel: FaceApiModels.DetectionModel);

                if (faces.Any())
                {
                    var faceId = faces.First().FaceId;
                    _logger.LogInformation("[{Timestamp}] Face detectada: {FaceId}", DateTime.UtcNow, faceId);
                    return (faceId, "Face detectada com sucesso.");
                }

                _logger.LogWarning("[{Timestamp}] Nenhuma face detectada.", DateTime.UtcNow);
                return (null, "Nenhuma face detectada.");
            }
            catch (APIErrorException ex)
            {
                _logger.LogError(ex, "[{Timestamp}] Erro na API: {Message}", DateTime.UtcNow, ex.Body?.Error?.Message);
                return (null, $"Erro na API: {ex.Body?.Error?.Message}");
            }
        }

        public async Task<(bool isIdentical, double confidence)> VerifyFaces(Guid faceId1, Guid faceId2)
        {
            var client = GetClient();
            try
            {
                var result = await client.Face.VerifyFaceToFaceAsync(faceId1, faceId2);
                _logger.LogInformation("[{Timestamp}] Verificação concluída. Confiança: {Confidence}", DateTime.UtcNow, result.Confidence);
                return (result.IsIdentical, result.Confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Timestamp}] Erro ao verificar faces.", DateTime.UtcNow);
                return (false, 0);
            }
        }

        public async Task<Guid?> IdentifyFaceAsync(Guid faceId)
        {
            var client = GetClient();
            try
            {
                await EnsurePersonGroupExistsAsync();
                var results = await client.Face.IdentifyAsync(new List<Guid> { faceId }, _personGroupId);
                var candidate = results.FirstOrDefault()?.Candidates.FirstOrDefault();

                if (candidate != null && candidate.Confidence > 0.5)
                {
                    _logger.LogInformation("[{Timestamp}] Pessoa identificada: {PersonId} com confiança {Confidence}", DateTime.UtcNow, candidate.PersonId, candidate.Confidence);
                    return candidate.PersonId;
                }

                _logger.LogWarning("[{Timestamp}] Nenhuma correspondência encontrada.", DateTime.UtcNow);
                return null;
            }
            catch (APIErrorException ex)
            {
                _logger.LogError(ex, "[{Timestamp}] Erro na API (Identificar): {Message}", DateTime.UtcNow, ex.Body?.Error?.Message);
                return null;
            }
        }

        public async Task<Guid> CreatePersonAsync(string name)
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            var person = await client.PersonGroupPerson.CreateAsync(_personGroupId, name);
            _logger.LogInformation("[{Timestamp}] Pessoa '{Name}' criada com ID: {PersonId}", DateTime.UtcNow, name, person.PersonId);
            return person.PersonId;
        }

        public async Task<PersistedFace> AddFaceToPersonAsync(Guid personId, Stream imageStream)
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            imageStream.Position = 0;

            var persistedFace = await client.PersonGroupPerson.AddFaceFromStreamAsync(
                _personGroupId, personId, imageStream, detectionModel: FaceApiModels.DetectionModel);

            _logger.LogInformation("[{Timestamp}] Face adicionada à pessoa {PersonId}. PersistedFaceId: {PersistedFaceId}", DateTime.UtcNow, personId, persistedFace.PersistedFaceId);
            return persistedFace;
        }

        public async Task TrainPersonGroupAsync()
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            _logger.LogInformation("[{Timestamp}] Iniciando treino do grupo: {PersonGroupId}", DateTime.UtcNow, _personGroupId);
            await client.PersonGroup.TrainAsync(_personGroupId);
        }

        public async Task<TrainingStatus> GetTrainingStatusAsync()
        {
            var client = GetClient();
            return await client.PersonGroup.GetTrainingStatusAsync(_personGroupId);
        }

        public async Task<PersonGroup> GetPersonGroupAsync()
        {
            var client = GetClient();
            _logger.LogInformation("[{Timestamp}] Obtendo detalhes do grupo '{PersonGroupId}'.", DateTime.UtcNow, _personGroupId);
            return await client.PersonGroup.GetAsync(_personGroupId, returnRecognitionModel: true);
        }
        public async Task<Guid?> DetectFace(Stream imageStream)
        {
            var (faceId, _) = await DetectFaceWithFeedback(imageStream);
            return faceId;
        }
        public async Task<string> DetectFaceAndGetId(Stream imageStream)
        {
            var (faceId, _) = await DetectFaceWithFeedback(imageStream);
            return faceId?.ToString();
        }

        public async Task DeletePersonGroupAsync()
        {
            var client = GetClient();
            try
            {
                await client.PersonGroup.DeleteAsync(_personGroupId);
                _logger.LogInformation("[{Timestamp}] Grupo '{PersonGroupId}' apagado com sucesso.", DateTime.UtcNow, _personGroupId);
            }
            catch (APIErrorException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[{Timestamp}] Tentativa de apagar grupo '{PersonGroupId}', mas ele não foi encontrado.", DateTime.UtcNow, _personGroupId);
            }
        }
    }
}
