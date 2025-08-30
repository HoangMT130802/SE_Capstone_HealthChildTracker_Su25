using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Entities;
using Repositories.Interfaces;

namespace KidTracking.API.Controllers
{
    /// <summary>
    /// Controller để test và validate Multi-Disease Vaccine functionality
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MultiDiseaseVaccineTestController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MultiDiseaseVaccineTestController> _logger;

        public MultiDiseaseVaccineTestController(
            IUnitOfWork unitOfWork,
            ILogger<MultiDiseaseVaccineTestController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách vaccine và số bệnh mà chúng có thể chữa
        /// </summary>
        [HttpGet("vaccines-with-diseases")]
        public async Task<ActionResult> GetVaccinesWithDiseases()
        {
            try
            {
                var vaccineRepo = _unitOfWork.GetRepository<Vaccine>();
                var vaccines = await vaccineRepo.FindAsync(
                    v => v.Status == "active",
                    includeProperties: "VaccineDiseases,VaccineDiseases.Disease"
                );

                var result = vaccines.Select(v => new
                {
                    VaccineId = v.VaccineId,
                    VaccineName = v.Name,
                    DiseaseCount = v.VaccineDiseases?.Count ?? 0,
                    IsMultiDisease = (v.VaccineDiseases?.Count ?? 0) > 1,
                    Diseases = v.VaccineDiseases?.Select(vd => new
                    {
                        DiseaseId = vd.DiseaseId,
                        DiseaseName = vd.Disease?.Name ?? "Unknown"
                    }).Cast<object>().ToList() ?? new List<object>()
                }).OrderByDescending(v => v.DiseaseCount).ToList();

                return Ok(new
                {
                    message = "Danh sách vaccine và diseases",
                    totalVaccines = result.Count,
                    multiDiseaseVaccines = result.Count(v => v.IsMultiDisease),
                    singleDiseaseVaccines = result.Count(v => !v.IsMultiDisease),
                    vaccines = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách vaccines with diseases");
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra ChildVaccineProfile cho một appointment cụ thể
        /// </summary>
        [HttpGet("appointment/{appointmentId}/child-vaccine-profiles")]
        public async Task<ActionResult> GetChildVaccineProfilesByAppointment(int appointmentId)
        {
            try
            {
                var cvpRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profiles = await cvpRepo.FindAsync(
                    p => p.AppointmentId == appointmentId,
                    includeProperties: "Child,Vaccine,Disease,Appointment"
                );

                if (!profiles.Any())
                {
                    return NotFound(new { message = $"Không tìm thấy ChildVaccineProfile cho AppointmentId {appointmentId}" });
                }

                var result = profiles.Select(p => new
                {
                    ProfileId = p.VaccineProfileId,
                    ChildId = p.ChildId,
                    ChildName = p.Child?.FullName ?? "Unknown",
                    VaccineId = p.VaccineId,
                    VaccineName = p.Vaccine?.Name ?? "Unknown",
                    DiseaseId = p.DiseaseId,
                    DiseaseName = p.Disease?.Name ?? "Unknown",
                    DoseNum = p.DoseNum,
                    Status = p.Status,
                    ExpectedDate = p.ExpectedDate,
                    ActualDate = p.ActualDate,
                    AppointmentId = p.AppointmentId
                }).OrderBy(p => p.DiseaseId).ToList();

                var groupedByVaccine = result.GroupBy(p => new { p.VaccineId, p.VaccineName })
                    .Select(g => new
                    {
                        VaccineId = g.Key.VaccineId,
                        VaccineName = g.Key.VaccineName,
                        DiseaseCount = g.Count(),
                        IsMultiDisease = g.Count() > 1,
                        Diseases = g.Select(p => new
                        {
                            DiseaseId = p.DiseaseId,
                            DiseaseName = p.DiseaseName,
                            Status = p.Status,
                            ProfileId = p.ProfileId
                        }).ToList()
                    }).ToList();

                return Ok(new
                {
                    message = $"ChildVaccineProfile cho AppointmentId {appointmentId}",
                    appointmentId = appointmentId,
                    totalProfiles = result.Count,
                    profilesByVaccine = groupedByVaccine,
                    allProfiles = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy ChildVaccineProfile cho appointment {AppointmentId}", appointmentId);
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra tất cả ChildVaccineProfile của một child cho một vaccine cụ thể
        /// </summary>
        [HttpGet("child/{childId}/vaccine/{vaccineId}/profiles")]
        public async Task<ActionResult> GetChildVaccineProfilesByChildAndVaccine(int childId, int vaccineId)
        {
            try
            {
                var cvpRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profiles = await cvpRepo.FindAsync(
                    p => p.ChildId == childId && p.VaccineId == vaccineId,
                    includeProperties: "Child,Vaccine,Disease,Appointment"
                );

                if (!profiles.Any())
                {
                    return NotFound(new { message = $"Không tìm thấy ChildVaccineProfile cho Child {childId} và Vaccine {vaccineId}" });
                }

                var result = profiles.Select(p => new
                {
                    ProfileId = p.VaccineProfileId,
                    DiseaseId = p.DiseaseId,
                    DiseaseName = p.Disease?.Name ?? "Unknown",
                    DoseNum = p.DoseNum,
                    Status = p.Status,
                    ExpectedDate = p.ExpectedDate,
                    ActualDate = p.ActualDate,
                    AppointmentId = p.AppointmentId,
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(p.CreatedAt).DateTime,
                    UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(p.UpdatedAt).DateTime
                }).OrderBy(p => p.DiseaseId).ThenBy(p => p.DoseNum).ToList();

                var diseaseGroups = result.GroupBy(p => new { p.DiseaseId, p.DiseaseName })
                    .Select(g => new
                    {
                        DiseaseId = g.Key.DiseaseId,
                        DiseaseName = g.Key.DiseaseName,
                        TotalDoses = g.Count(),
                        CompletedDoses = g.Count(p => p.Status == "Completed"),
                        Doses = g.OrderBy(p => p.DoseNum).ToList()
                    }).ToList();

                return Ok(new
                {
                    message = $"ChildVaccineProfile cho Child {childId} và Vaccine {vaccineId}",
                    childId = childId,
                    vaccineId = vaccineId,
                    totalProfiles = result.Count,
                    diseaseCount = diseaseGroups.Count,
                    isMultiDisease = diseaseGroups.Count > 1,
                    diseaseGroups = diseaseGroups,
                    allProfiles = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy ChildVaccineProfile cho Child {ChildId} và Vaccine {VaccineId}", childId, vaccineId);
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        /// <summary>
        /// Thống kê Multi-Disease Vaccine trong hệ thống
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult> GetMultiDiseaseVaccineStatistics()
        {
            try
            {
                var vaccineRepo = _unitOfWork.GetRepository<Vaccine>();
                var cvpRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                
                // Thống kê vaccine
                var vaccines = await vaccineRepo.FindAsync(
                    v => v.Status == "active",
                    includeProperties: "VaccineDiseases"
                );

                var vaccineStats = new
                {
                    TotalActiveVaccines = vaccines.Count(),
                    SingleDiseaseVaccines = vaccines.Count(v => (v.VaccineDiseases?.Count ?? 0) <= 1),
                    MultiDiseaseVaccines = vaccines.Count(v => (v.VaccineDiseases?.Count ?? 0) > 1),
                    MaxDiseasesPerVaccine = vaccines.Max(v => v.VaccineDiseases?.Count ?? 0)
                };

                // Thống kê ChildVaccineProfile
                var allProfiles = await cvpRepo.GetAllAsync(includeProperties: "");
                var profilesList = allProfiles?.ToList() ?? new List<ChildVaccineProfile>();
                var profileStats = new
                {
                    TotalProfiles = profilesList.Count,
                    CompletedProfiles = profilesList.Count(p => p.Status == "Completed"),
                    PendingProfiles = profilesList.Count(p => p.Status == "Pending"),
                    ScheduledProfiles = profilesList.Count(p => p.Status == "Scheduled")
                };

                // Top Multi-Disease Vaccines
                var topMultiDiseaseVaccines = vaccines
                    .Where(v => (v.VaccineDiseases?.Count ?? 0) > 1)
                    .Select(v => new
                    {
                        VaccineId = v.VaccineId,
                        VaccineName = v.Name,
                        DiseaseCount = v.VaccineDiseases?.Count ?? 0
                    })
                    .OrderByDescending(v => v.DiseaseCount)
                    .Take(10)
                    .ToList();

                return Ok(new
                {
                    message = "Thống kê Multi-Disease Vaccine",
                    vaccineStatistics = vaccineStats,
                    profileStatistics = profileStats,
                    topMultiDiseaseVaccines = topMultiDiseaseVaccines,
                    generatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thống kê Multi-Disease Vaccine");
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }
    }
}
