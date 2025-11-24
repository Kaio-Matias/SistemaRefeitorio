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

        // === MÉTODO PRINCIPAL DE IDENTIFICAÇÃO (1 para N) ===
        public async Task<Guid?> IdentificarFaceAsync(Stream imageStream)
        {
            try
            {
                imageStream.Position = 0;
                // Passo 1: Detectar a face na imagem enviada
                var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(
                    imageStream,
                    recognitionModel: FaceApiModels.RecognitionModel,
                    detectionModel: FaceApiModels.DetectionModel
                );

                var faceIds = detectedFaces.Select(f => f.FaceId.Value).ToList();

                if (!faceIds.Any())
                {
                    _logger.LogWarning("Nenhuma face detectada na imagem para identificação.");
                    return null;
                }

                // Passo 2: Identificar a quem pertence a face dentro do PersonGroup
                var identifyResults = await _faceClient.Face.IdentifyAsync(faceIds, _personGroupId);

                foreach (var result in identifyResults)
                {
                    if (result.Candidates.Any())
                    {
                        // Pega o candidato com maior confiança
                        var bestCandidate = result.Candidates.OrderByDescending(c => c.Confidence).First();
                        _logger.LogInformation("Face identificada como PersonId {PersonId} com confiança {Confidence}", bestCandidate.PersonId, bestCandidate.Confidence);
                        return bestCandidate.PersonId;
                    }
                }

                _logger.LogWarning("Face detectada, mas não corresponde a nenhum colaborador no grupo (Desconhecido).");
                return null;
            }
            catch (APIErrorException ex)
            {
                _logger.LogError(ex, "Erro na API de Face durante a identificação: {Message}", ex.Body?.Error?.Message);
                throw;
            }
        }

        // === GERENCIAMENTO E TREINAMENTO ===

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

        public async Task<TrainingStatus> GetTrainingStatusAsync()
        {
            return await _faceClient.PersonGroup.GetTrainingStatusAsync(_personGroupId);
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

        // Útil para validar se há face antes de tentar cadastrar, mantido como utilitário
        public async Task<(Guid? faceId, string message)> DetectFaceWithFeedback(Stream imageStream)
        {
            try
            {
                imageStream.Position = 0;
                var faces = await _faceClient.Face.DetectWithStreamAsync(imageStream, recognitionModel: FaceApiModels.RecognitionModel, detectionModel: FaceApiModels.DetectionModel);

                if (faces.Any())
                {
                    var faceId = faces.First().FaceId;
                    return (faceId, "Face detectada com sucesso.");
                }

                return (null, "Nenhuma face detectada.");
            }
            catch (APIErrorException ex)
            {
                _logger.LogError(ex, "Erro na API ao detectar face.");
                return (null, $"Erro na API: {ex.Body?.Error?.Message}");
            }
        }
        // DENTRO DE FaceApiService.cs

        public async Task<PersonGroup> GetPersonGroupAsync()
        {
            _logger.LogInformation("[{Timestamp}] Obtendo detalhes do grupo '{PersonGroupId}'.", DateTime.UtcNow, _personGroupId);
            return await _faceClient.PersonGroup.GetAsync(_personGroupId, returnRecognitionModel: true);
        }
    }
}