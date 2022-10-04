using System;
using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
    using LinkEnum = EVLinks;
    enum EVLinks
    {
        /// <summary>
        /// Link to next EVBLOCK (linked list) (can be NIL)
        /// </summary>
        ev_ev_next,
        /// <summary>
        /// Referencing link to EVBLOCK with parent event (can be NIL). The parent relationship must not contain circular references.
        /// The scope of the parent event must be larger than or equal to the scope of the child event. If the child does not define its own scope, then the scope of the parent 
        /// is used (see ev_scope).
        /// Note: in contrast to ev_ev_range, there is no restriction on the type of the parent event. Common use cases are that trigger or interrupt events have a recording (begin) event 
        /// as parent. For a template EVBLOCK of an event signal group it is possible to reference a CGBLOCK containing all the parent signals. (see chapter 4.12.5 Event Signals for details)
        /// </summary>
        ev_ev_parent,
        /// <summary>
        /// Referencing link to EVBLOCK with event that defines the beginning of a range (can be NIL, must be NIL if ev_range_type ≠ 2).
        /// The event referenced by ev_ev_range and the current event define the borders of a range. ev_ev_range must define the beginning of the range (i.e. ev_range_type = 1) and 
        /// the current event its end. This implies the following restrictions:
        /// ev_ev_range must have occurred prior to the current event, i.e. the (calculated) synchronization value for ev_ev_range must be smaller than for the current event. 
        /// Furthermore, both events must have the same event type and sync type and the same parent, i.e. the values of ev_type, ev_sync_type and ev_parent must be equal.
        /// In addition, both events must have the same scope, which is achieved by the rule, that the current event must re-use the scope list of ev_ev_range (see explanation for ev_scope). 
        /// This avoids a duplication of the scope list.
        /// </summary>
        ev_ev_range,
        /// <summary>
        /// Pointer to TXBLOCK with event name (can be NIL) Name must be according to naming rules stated in Naming Rules. If available, the name of a named trigger condition 
        /// should be used as event name. Other event types may have individual names or no names.
        /// </summary>
        ev_tx_name,
        /// <summary>
        /// Pointer to TX/MDBLOCK with event comment and additional information, e.g. trigger condition or formatted user comment text (can be NIL)
        /// </summary>
        ev_md_comment,
        linkcount
    };

    /// <summary>
    /// Event Block
    /// </summary>
    class EVBlock : BaseBlock
    {
        /// <summary>
        /// List of links to channels and channel groups to which the event applies (referencing links to CNBLOCKs or CGBLOCKs). This defines the "scope" of the event.
        /// The length of the list is given by ev_scope_count.It can be empty(ev_scope_count = 0).
        /// If the record index is used for synchronization(ev_sync_type = 1), then the scope must be less than or equal to one channel group, i.e.all affected channels must be in one channel group.
        /// If this event defines the end of a range(ev_range_type = 2) and references the event for the beginning of the range (ev_ev_range ≠ NIL), then the scope list must be 
        /// empty and this event must use the scope list of the event referenced by ev_ev_range.Thus, both events have the same scope.
        /// If this event has a parent event (ev_parent ≠ NIL), and if its scope list is empty (and also the one of ev_ev_range ≠ NIL), then the scope list of ev_parent must be used.
        /// For all other cases, an empty scope list means that the event has a global scope, i.e.the event applies to the whole file.
        /// </summary>
        public Int64 ev_scopeGet(int index) => links[(int)LinkEnum.linkcount + index];
        public void ev_scopeSet(int index, Int64 value) => links[(int)LinkEnum.linkcount + index] = value;

        /// <summary>
        /// List of attachments for this event (references to ATBLOCKs in global linked list of ATBLOCKs).
        /// The length of the list is given by ev_attachment_count. It can be empty (ev_attachment_count = 0), i.e. there are no attachments for this event.
        /// </summary>
        public Int64 ev_at_referenceGet(int index) => links[(int)((int)LinkEnum.linkcount + data.ev_scope_count + index)];
        public void ev_at_referenceSet(int index, Int64 value) => links[(int)((int)LinkEnum.linkcount + data.ev_scope_count + index)] = value;

        /// <summary>
        /// Only present if the “Group name present” (bit 1) flag is set. Pointer to TXBLOCK with and arbitrary group name for the event. (can be NIL)
        /// Events with the same group name are for the same use case. For template EVBLOCKs of event signal groups, the group name must not be specified 
        /// since the name of the event structure already defines the use case. (see chapter 4.12.5 Event Signals). Valid since MDF 4.2.0
        /// </summary>
        public Int64 ev_tx_group_name
        {
            get => links[(int)((int)LinkEnum.linkcount + data.ev_scope_count + data.ev_attachment_count)];
            set => links[(int)((int)LinkEnum.linkcount + data.ev_scope_count + data.ev_attachment_count)] = value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Event type
            /// <br/>0 = Recording - This event type specifies a recording period, i.e.the first and last time a signal value could theoretically be recorded. Recording events must always 
            /// define a range, i.e. 1 ≤ ev_range_type ≤ 2. All recording events in the MDF file must only occur on exactly one scope level: either all have a global scope, e.g. if file 
            /// has been created by a single recorder; or all of them have a scope on CG level(ev_scope list must contain links to one or more CGBLOCKs), e.g.after combining two files.
            /// Recording events which apply to single channels only (CN level scope) generally is not possible. Within one scope, there can be several recording periods, e.g. if the 
            /// recording was paused between these periods (although this should be modeled by a recording interrupt event, see below). However, these periods must not overlap.
            /// <br/>1 = Recording Interrupt - This event type indicates that the recording has been interrupted.It can only occur within the range of a matching recording period.
            /// Like the recording event type, it can only have a global scope or a CG level scope. It should be either on the same scope level as the recording event, or on a lower level, 
            /// i.e. if the recording events have a global scope, the recording interrupt event can either have a global or a CG level scope. A recording interrupt can be defined as point 
            /// single event) or as range (pair of events as explained for ev_ev_range). If it defines a point, the interruption should be considered to automatically have finished when the 
            /// next sample for a signal value for a channel in its scope level occurred. Since a recording interrupt event is closely related to a recording period, its ev_parent should 
            /// point to the respective "range begin" recording event.
            /// <br/>2 = Acquisition Interrupt - This event type indicates that not only the recording, but already the acquisition of the signal values has been interrupted.Except of this, 
            /// the same rules apply as for the recording interrupt event type.
            /// <br/>3 = Start Recording Trigger - This event type specifies an event which started the recording of signal values due to some condition(including user interaction). 
            /// The trigger condition can be specified in the ev_md_comment MDBLOCK, (see Table 30). Here also a pre and post trigger interval can be specified. A start recording trigger event 
            /// can only occur as point(ev_range_type = 0), not as range.It usually is closely related to a recording period, i.e.it should have the same scope as the respective recording 
            /// events(note that due to the pre-trigger interval, the matching "range begin" recording event can be before this event). Here ev_parent may be used to point to the "range begin" 
            /// recording event. For some other use case, ev_parent instead might point to another trigger event, e.g. if this trigger activated the condition for the start recording trigger.
            /// <br/>4 = Stop Recording Trigger - Symmetrically to the "start recording trigger" event type, this event type specifies an event which stopped the recording of signal values 
            /// due to some condition.The same rules apply as for the start recording trigger event type. Note that the two event types may occur in pairs, but they do not have to.
            /// <br/>5 = Trigger - This event type generally specifies an event that occurred due to some condition(except of the special start and stop recording trigger event types). 
            /// The trigger condition can be specified in the ev_md_comment MDBLOCK, (see Table 30). A trigger event can only occur as point(ev_range_type = 0), not as range.
            /// <br/>6 = Marker - This event type specifies a marker for a point or a range.As examples, a marker could be a user-generated comment or an automatically generated bookmark.
            /// </summary>
            public byte ev_type;

            /// <summary>
            /// Sync type
            /// <br/>1 = calculated synchronization value represents time in seconds 
            /// <br/>2 = calculated synchronization value represents angle in radians 
            /// <br/>3 = calculated synchronization value represents distance in meter 
            /// <br/>4 = calculated synchronization value represents zero-based record index
            /// <br/>For ev_sync_type&lt; 4, the scope of the event must either be global or it must only contain channel groups(or channels from channel groups) which are in the same 
            /// synchronization domain, i.e.which contain a(virtual) master channel with matching cn_sync_type.In case of a global scope, the scope automatically is restricted to 
            /// channel groups which are in the same synchronization domain. For ev_sync_type = 4, the scope of the event must be less than or equal to a single channel group, i.e.
            /// the channel group whose record index is used for synchronization. See also 4.4.6 Synchronization Domains.
            /// </summary>
            public byte ev_sync_type;

            /// <summary>
            /// Range type:
            /// <br/>0 = event defines a point
            /// <br/>1 = event defines the beginning of a range
            /// <br/>2 = event defines the end of a range
            /// <br/>Point and range are defined in the synchronization domain defined by ev_sync_type.
            /// </summary>
            public byte ev_range_type;

            /// <summary>
            /// Cause of event
            /// <br/>0 = OTHER - cause of event is not known or does not fit into given categories.
            /// <br/>1 = ERROR - event was caused by some error.
            /// <br/>2 = TOOL - event was caused by tool-internal condition, e.g.trigger condition or re-configuration.
            /// <br/>3 = SCRIPT - event was caused by a scripting command.
            /// <br/>4 = USER - event was caused directly by user, e.g.user input or some other interaction with GUI.
            /// </summary>
            public byte ev_cause;

            /// <summary>
            /// Flags - The value contains the following bit flags(Bit 0 = LSB) :
            /// <br/>Bit 0: Post processing flag - If set, the event has been generated during post processing of the file.
            /// <br/>Bit 1: Group name present flag - If set, this indicates that an event group name is specified by means of the ev_tx_group_name link.
            /// <br/>Valid since MDF 4.2.0, should not be set for earlier versions.
            /// </summary>
            public byte ev_flags;

            /// <summary>
            /// Reserved
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 3)]
            byte[] ev_reserved;

            /// <summary>
            /// Length M of ev_scope list. Can be zero.
            /// </summary>
            public UInt32 ev_scope_count;

            /// <summary>
            /// Length N of ev_at_reference list, i.e. number of attachments for this event. Can be zero.
            /// </summary>
            public UInt16 ev_attachment_count;

            /// <summary>
            /// Creator index, i.e. zero-based index of FHBLOCK in global list of FHBLOCKs that specifies which application has created or changed this event (e.g. when generating event offline).
            /// </summary>
            public UInt16 ev_creator_index;

            /// <summary>
            /// Base value for synchronization value.
            /// The synchronization value of the event is the product of base value and factor (ev_sync_base_value x ev_sync_factor). See also remark for ev_sync_factor.
            /// The calculated synchronization value depends on ev_sync_type:
            /// For ev_sync_type&lt; 4, the synchronization value is a time/angle/distance value relative to the respective start value in HDBLOCK.Negative synchronization values can be 
            /// used for events that occurred before the start value.
            /// For ev_sync_type = 4, the synchronization value is the absolute record index in the channel group specified by the scope.The event thus occurred at the same time as 
            /// the record indicated by the index.In this case, the value must be rounded to an Integer value ≥ 0 and&lt;cg_cycle_count, i.e.it must be less than the number of cycles 
            /// specified for the channel group. See also 4.4.6 Synchronization Domains.
            /// </summary>
            public Int64 ev_sync_base_value;

            /// <summary>
            /// Factor for event synchronization value.
            /// The event synchronization value is the product of base value and factor(ev_sync_base_value x ev_sync_factor).
            /// Generally, base value and factor can be chosen arbitrarily, but as recommendation they should be used to reflect the raw value and conversion factor of the master channel 
            /// to be synchronized with.For instance, assume a time master channel using a 64-bit Integer channel data type to store the raw time value in nanoseconds using a linear 
            /// conversion with offset 0 and factor 1e-9. In this case, ev_sync_factor could be set to 1e-9 and ev_sync_base_value could store the nanosecond time stamp value.
            /// Thus, a higher precision is available than when just specifying the time value in seconds as REAL value. For ev_sync_type = 4, ev_sync_factor generally could be set to 1.0, 
            /// so that the record index will be given by ev_sync_base_value.
            /// </summary>
            public double ev_sync_factor;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        // Objects to direct access childs
        public EVBlock ev_next => links.GetObject(LinkEnum.ev_ev_next);
        public EVBlock ev_parent => links.GetObject(LinkEnum.ev_ev_parent);
        public EVBlock ev_range => links.GetObject(LinkEnum.ev_ev_range);
        public TXBlock tx_name => links.GetObject(LinkEnum.ev_tx_name);
        public MDBlock md_comment => links.GetObject(LinkEnum.ev_md_comment);

        public EVBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
        }
    };
}
