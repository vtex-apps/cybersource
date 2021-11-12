using System;
using System.Xml.Serialization;

namespace Cybersource.Models
{

	[XmlRoot(ElementName = "Note", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
	public class ReviewerNote
	{
		[XmlAttribute(AttributeName = "Date")]
		public string Date { get; set; }
		[XmlAttribute(AttributeName = "AddedBy")]
		public string AddedBy { get; set; }
		[XmlAttribute(AttributeName = "Comment")]
		public string Comment { get; set; }
	}

	[XmlRoot(ElementName = "Notes", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
	public class ReviewerNotes
	{
		[XmlElement(ElementName = "Note", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
		public ReviewerNote Note { get; set; }
	}

	[XmlRoot(ElementName = "Update", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
	public class Update
	{
		[XmlElement(ElementName = "OriginalDecision", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
		public string OriginalDecision { get; set; }
		[XmlElement(ElementName = "NewDecision", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
		public string NewDecision { get; set; }
		[XmlElement(ElementName = "Reviewer", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
		public string Reviewer { get; set; }
		[XmlElement(ElementName = "ReviewerComments", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
		public string ReviewerComments { get; set; }
		[XmlElement(ElementName = "Notes", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
		public ReviewerNotes Notes { get; set; }
		[XmlElement(ElementName = "Queue", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
		public string Queue { get; set; }
		[XmlElement(ElementName = "Profile", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
		public string Profile { get; set; }
		[XmlAttribute(AttributeName = "MerchantReferenceNumber")]
		public string MerchantReferenceNumber { get; set; }
		[XmlAttribute(AttributeName = "RequestID")]
		public string RequestID { get; set; }
	}

	[XmlRoot(ElementName = "CaseManagementOrderStatus", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
	public class CaseManagementOrderStatus
	{
		[XmlElement(ElementName = "Update", Namespace = "http://reports.cybersource.com/reports/cmos/1.0")]
		public Update Update { get; set; }
		[XmlAttribute(AttributeName = "xmlns")]
		public string Xmlns { get; set; }
		[XmlAttribute(AttributeName = "MerchantID")]
		public string MerchantID { get; set; }
		[XmlAttribute(AttributeName = "Name")]
		public string Name { get; set; }
		[XmlAttribute(AttributeName = "Date")]
		public string Date { get; set; }
		[XmlAttribute(AttributeName = "Version")]
		public string Version { get; set; }
	}

}
