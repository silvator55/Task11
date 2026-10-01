using Microsoft.AspNetCore.Mvc;
using HospitalSystem.Models;

namespace HospitalSystem.Controllers
{
    public class AppointmentController : Controller
    {
        
        private static List<Doctor> _doctors = new List<Doctor>
        {
            new Doctor { Id = 1, Name = "Dr. Amr Mohamed ", Specialization = "Cardiology", Image = "/images/FirstDoctor.jpeg" },
            new Doctor { Id = 2, Name = "Dr. Adel Shakal", Specialization = "Pediatrics", Image = "/images/Dola2.jpg" },
            new Doctor { Id = 3, Name = "Dr. Tamer Ahmed ", Specialization = "Dermatology", Image = "/images/doctor1.jpg" },
            new Doctor { Id = 4, Name = "Dr. Khaled Omar", Specialization = "Dentist", Image = "/images/doctor2.jpg" },
            new Doctor { Id = 5, Name = "Dr. Ebrahim Youssef", Specialization = "Neurology", Image = "/images/doctor3.jpg" },
            new Doctor { Id = 6, Name = "Dr. Omar Mohamed", Specialization = "Orthopedist", Image = "/images/doctor4.jpg" },
            new Doctor { Id = 7, Name = "Dr. Ahmed Mohamed", Specialization = "General Surgery", Image = "/images/doctor6.jpg" },
            new Doctor { Id = 8, Name = "Dr. Zeyad Mohamed", Specialization = "ENT specialist", Image = "/images/doctor2.jpg" }
        };

        private static List<Appointment> _appointments = new List<Appointment>();

        [HttpGet]
        public IActionResult Index()
        {
            return View(_appointments);
        }

        public IActionResult Create(string? name, string? specialization, int page = 1)
        {
            int pageSize = 4;
            var query = _doctors.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(d => d.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(specialization))
            {
                query = query.Where(d => d.Specialization.Equals(specialization, StringComparison.OrdinalIgnoreCase));
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var doctorsList = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Name = name;
            ViewBag.Specialization = specialization;

            return View(doctorsList);
        }

        [HttpGet]
        public IActionResult Book(int doctorId)
        {
            var doctor = _doctors.FirstOrDefault(d => d.Id == doctorId);
            if (doctor == null)
            {
                return NotFound();
            }

            DateTime targetDate = DateTime.Today.AddDays(1);

            TimeSpan firstAvailableTime = GetNextAvailableSlot(doctor.Id, targetDate);

            var appointment = new Appointment
            {
                DoctorId = doctor.Id,
                Doctor = doctor,
                Date = targetDate,
                Time = firstAvailableTime
            };

            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Book(Appointment appointment)
        {
            var doctor = _doctors.FirstOrDefault(d => d.Id == appointment.DoctorId);
            appointment.Doctor = doctor;

            TimeSpan workStart = new TimeSpan(9, 0, 0);
            TimeSpan workEnd = new TimeSpan(16, 30, 0);   

            if (appointment.Date.Date < DateTime.Today)
            {
                ModelState.AddModelError("Date", "You cannot book an appointment in the past!");
            }

            if (appointment.Time < workStart || appointment.Time > workEnd)
            {
                ModelState.AddModelError("Time", "Working hours are only from 09:00 AM to 04:30 PM.");
            }

            var isTimeConflicting = _appointments.Any(a =>
                a.DoctorId == appointment.DoctorId &&
                a.Date.Date == appointment.Date.Date &&
                Math.Abs((a.Time - appointment.Time).TotalMinutes) < 30
            );

            if (isTimeConflicting)
            {
                TimeSpan suggestedTime = GetNextAvailableSlot(appointment.DoctorId, appointment.Date);
                string formattedSuggestedTime = DateTime.Today.Add(suggestedTime).ToString("hh:mm tt");

                ModelState.AddModelError("Time", $"This time is unavailable! Next available slot for this doctor is at {formattedSuggestedTime}.");
            }

            if (ModelState.IsValid)
            {
                appointment.Id = _appointments.Count + 1;
                _appointments.Add(appointment);

                return RedirectToAction(nameof(Index));
            }

            return View(appointment);
        }

        private TimeSpan GetNextAvailableSlot(int doctorId, DateTime date)
        {
            TimeSpan slot = new TimeSpan(9, 0, 0); 
            TimeSpan end = new TimeSpan(16, 30, 0); 

            while (slot <= end)
            {
                bool isBooked = _appointments.Any(a =>
                    a.DoctorId == doctorId &&
                    a.Date.Date == date.Date &&
                    Math.Abs((a.Time - slot).TotalMinutes) < 30);

                if (!isBooked)
                {
                    return slot; 
                }

                slot = slot.Add(TimeSpan.FromMinutes(30)); 
            }

            return new TimeSpan(9, 0, 0); 
        }
    }
}