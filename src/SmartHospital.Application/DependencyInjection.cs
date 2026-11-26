using Microsoft.Extensions.DependencyInjection;
using SmartHospital.Application.Billing;
using SmartHospital.Application.Clinical;
using SmartHospital.Application.Inpatient;
using SmartHospital.Application.Laboratory;
using SmartHospital.Application.MasterData;
using SmartHospital.Application.Patients;
using SmartHospital.Application.Pharmacy;
using SmartHospital.Application.Reception;

namespace SmartHospital.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSmartHospitalApplication(this IServiceCollection services)
    {
        services.AddScoped<IMasterDataService, MasterDataService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IReceptionService, ReceptionService>();
        services.AddScoped<IClinicalService, ClinicalService>();
        services.AddScoped<ILabAndImagingService, LabAndImagingService>();
        services.AddScoped<IPharmacyAndInventoryService, PharmacyAndInventoryService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IInpatientAndHospitalOpsService, InpatientAndHospitalOpsService>();

        return services;
    }
}
