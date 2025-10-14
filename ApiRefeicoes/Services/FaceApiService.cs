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
        private readonly IFaceClient _faceClient;
        private readonly string _personGroupId;
        private readonly ILogger<FaceApiService> _logger;
        private readonly IConfiguration _configuration;

        public FaceApiService(IConfiguration configuration, ILogger<FaceApiService> logger)
        {
            _configuration = configuration;
            var apiKey = configuration["AzureFaceApi:ApiKey"];
            var endpoint = configuration["AzureFaceApi:Endpoint"];
            _personGroupId = configuration["AzureFaceApi:PersonGroupId"];
            _logger = logger;

            _faceClient = new FaceClient(new ApiKeyServiceClientCredentials(apiKey)) { Endpoint = endpoint };
        }

        public async Task EnsurePersonGroupExistsAsync()
        {
            try
            {
                await _faceClient.PersonGroup.GetAsync(_personGroupId, returnRecognitionModel: true);
                _logger.LogInformation("[{Timestamp}] Grupo '{PersonGroupId}' já existe.", DateTime.UtcNow, _personGroupId);
            }
            catch (APIErrorException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[{Timestamp}] Grupo '{PersonGroupId}' não encontrado. Criando novo com modelo {RecognitionModel}.", DateTime.UtcNow, _personGroupId, FaceApiModels.RecognitionModel);
                await _faceClient.PersonGroup.CreateAsync(
                    personGroupId: _personGroupId,
                    name: "Grupo de Colaboradores do Refeitório",
                    userData: "Grupo criado automaticamente",
                    recognitionModel: FaceApiModels.RecognitionModel
                );
                _logger.LogInformation("[{Timestamp}] Grupo '{PersonGroupId}' criado com sucesso.", DateTime.UtcNow, _personGroupId);
            }
        }

        // NOVO MÉTODO DE IDENTIFICAÇÃO (1 para N)
        public async Task<Guid?> IdentificarFaceAsync(Stream imageStream)
        {
            imageStream.Position = 0;
            var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(imageStream, recognitionModel: FaceApiModels.RecognitionModel, detectionModel: FaceApiModels.DetectionModel);
            var faceIds = detectedFaces.Select(f => f.FaceId.Value).ToList();

            if (!faceIds.Any())
            {
                _logger.LogWarning("Nenhuma face detectada na imagem para identificação.");
                return null;
            }

            var identifyResults = await _faceClient.Face.IdentifyAsync(faceIds, _personGroupId);

            foreach (var result in identifyResults)
            {
                if (result.Candidates.Any())
                {
                    var bestCandidate = result.Candidates.OrderByDescending(c => c.Confidence).First();
                    _logger.LogInformation("Face identificada como PersonId {PersonId} com confiança {Confidence}", bestCandidate.PersonId, bestCandidate.Confidence);
                    return bestCandidate.PersonId;
                }
            }

            _logger.LogWarning("Face detectada, mas não corresponde a nenhum colaborador no grupo.");
            return null;
        }

        // MÉTODO DE TREINAMENTO ATUALIZADO E MAIS ROBUSTO
        public async Task TrainPersonGroupAsync()
        {
            await EnsurePersonGroupExistsAsync();
            _logger.LogInformation("[{Timestamp}] Iniciando treino do grupo: {PersonGroupId}", DateTime.UtcNow, _personGroupId);
            await _faceClient.PersonGroup.TrainAsync(_personGroupId);

            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await GetTrainingStatusAsync();
                _logger.LogInformation("Status do treinamento do grupo '{PersonGroupId}': {Status}", _personGroupId, trainingStatus.Status);

                if (trainingStatus.Status == TrainingStatusType.Succeeded)
                {
                    _logger.LogInformation("Treinamento do grupo '{PersonGroupId}' concluído com sucesso.", _personGroupId);
                    break;
                }
                if (trainingStatus.Status == TrainingStatusType.Failed)
                {
                    _logger.LogError("Treinamento do grupo '{PersonGroupId}' falhou: {Message}", _personGroupId, trainingStatus.Message);
                    throw new Exception($"Treinamento do grupo de pessoas falhou: {trainingStatus.Message}");
                }
            }
        }

        // --- MÉTODOS ORIGINAIS MANTIDOS ---
        public async Task<(Guid? faceId, string message)> DetectFaceWithFeedback(Stream imageStream)
        {
            try
            {
                imageStream.Position = 0;
                var faces = await _faceClient.Face.DetectWithStreamAsync(imageStream, recognitionModel: FaceApiModels.RecognitionModel, detectionModel: FaceApiModels.DetectionModel);

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
                _logger.LogError(ex, "[{Timestamp}] Erro na API ao detectar face: {Message}", DateTime.UtcNow, ex.Body?.Error?.Message);
                return (null, $"Erro na API: {ex.Body?.Error?.Message}");
            }
        }

        public async Task<(bool isIdentical, double confidence)> VerifyFaces(Guid faceId1, Guid faceId2)
        {
            try
            {
                var result = await _faceClient.Face.VerifyFaceToFaceAsync(faceId1, faceId2);
                _logger.LogInformation("[{Timestamp}] Verificação Face-a-Face concluída. Confiança: {Confidence}", DateTime.UtcNow, result.Confidence);
                return (result.IsIdentical, result.Confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Timestamp}] Erro ao verificar faces.", DateTime.UtcNow);
                return (false, 0);
            }
        }

        public async Task<(bool isIdentical, double confidence)> VerifyFaceToPersonAsync(Guid faceId, Guid personId)
        {
            try
            {
                var result = await _faceClient.Face.VerifyFaceToPersonAsync(faceId, personId, _personGroupId);
                _logger.LogInformation("[{Timestamp}] Verificação 1:1 (faceId x personId) concluída para PersonId {PersonId}. Confiança: {Confidence}", DateTime.UtcNow, personId, result.Confidence);
                return (result.IsIdentical, result.Confidence);
            }
            catch (APIErrorException ex)
            {
                _logger.LogError(ex, "[{Timestamp}] Erro na API ao verificar Face-to-Person para PersonId {PersonId}: {Message}", DateTime.UtcNow, personId, ex.Body?.Error?.Message);
                return (false, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Timestamp}] Erro geral ao verificar Face-to-Person para PersonId {PersonId}.", DateTime.UtcNow, personId);
                return (false, 0);
            }
        }

        public async Task<Guid> CreatePersonAsync(string name)
        {
            await EnsurePersonGroupExistsAsync();
            var person = await _faceClient.PersonGroupPerson.CreateAsync(_personGroupId, name);
            _logger.LogInformation("[{Timestamp}] Pessoa '{Name}' criada com ID: {PersonId}", DateTime.UtcNow, name, person.PersonId);
            return person.PersonId;
        }

        public async Task<PersistedFace> AddFaceToPersonAsync(Guid personId, Stream imageStream)
        {
            await EnsurePersonGroupExistsAsync();
            imageStream.Position = 0;
            var persistedFace = await _faceClient.PersonGroupPerson.AddFaceFromStreamAsync(
                _personGroupId, personId, imageStream, detectionModel: FaceApiModels.DetectionModel);
            _logger.LogInformation("[{Timestamp}] Face adicionada à pessoa {PersonId}. PersistedFaceId: {PersistedFaceId}", DateTime.UtcNow, personId, persistedFace.PersistedFaceId);
            return persistedFace;
        }

        public async Task<TrainingStatus> GetTrainingStatusAsync()
        {
            return await _faceClient.PersonGroup.GetTrainingStatusAsync(_personGroupId);
        }

        public async Task<PersonGroup> GetPersonGroupAsync()
        {
            _logger.LogInformation("[{Timestamp}] Obtendo detalhes do grupo '{PersonGroupId}'.", DateTime.UtcNow, _personGroupId);
            return await _faceClient.PersonGroup.GetAsync(_personGroupId, returnRecognitionModel: true);
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
            try
            {
                await _faceClient.PersonGroup.DeleteAsync(_personGroupId);
                _logger.LogInformation("[{Timestamp}] Grupo '{PersonGroupId}' apagado com sucesso.", DateTime.UtcNow, _personGroupId);
            }
            catch (APIErrorException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[{Timestamp}] Tentativa de apagar grupo '{PersonGroupId}', mas ele não foi encontrado.", DateTime.UtcNow, _personGroupId);
            }
        }
    }
}