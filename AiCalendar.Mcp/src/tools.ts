import { CalendarDatabase, CalendarEvent } from './database.js';

export class CalendarTools {
  constructor(private database: CalendarDatabase) {}

  async saveEvent(args: any) {
    try {
      const { title, start, end, timezone, location, attendees, notes, client_reference_id } = args;

      // Validate required fields
      if (!title || !start || !end) {
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                ok: false,
                error: 'Missing required fields: title, start, and end are required',
                data: null
              })
            }
          ]
        };
      }

      const event: CalendarEvent = {
        title: String(title).trim().substring(0, 255), // Trim and clamp title
        start,
        end,
        timezone: timezone || 'UTC',
        location: location ? String(location).trim().substring(0, 255) : undefined,
        attendees: Array.isArray(attendees) ? attendees : [],
        notes: notes ? String(notes).trim().substring(0, 1000) : undefined,
        client_reference_id: client_reference_id ? String(client_reference_id) : undefined
      };

      const savedEvent = await this.database.saveEvent(event);

      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              ok: true,
              data: {
                id: savedEvent.id,
                title: savedEvent.title,
                start: savedEvent.start,
                end: savedEvent.end,
                timezone: savedEvent.timezone
              },
              error: null
            })
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              ok: false,
              error: error instanceof Error ? error.message : 'Unknown error occurred',
              data: null
            })
          }
        ]
      };
    }
  }

  async updateEvent(args: any) {
    try {
      const { id, client_reference_id, ...updates } = args;

      if (!id && !client_reference_id) {
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                ok: false,
                error: 'Either id or client_reference_id must be provided',
                data: null
              })
            }
          ]
        };
      }

      let event: CalendarEvent | null = null;

      if (id) {
        event = await this.database.findById(Number(id));
        if (event) {
          await this.database.updateEventById(Number(id), updates);
          event = await this.database.findById(Number(id));
        }
      } else if (client_reference_id) {
        event = await this.database.findByClientReferenceId(String(client_reference_id));
        if (event && event.id) {
          await this.database.updateEventById(event.id, updates);
          event = await this.database.findById(event.id);
        }
      }

      if (!event) {
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                ok: false,
                error: 'Event not found',
                data: null
              })
            }
          ]
        };
      }

      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              ok: true,
              data: {
                id: event.id,
                title: event.title,
                start: event.start,
                end: event.end,
                timezone: event.timezone
              },
              error: null
            })
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              ok: false,
              error: error instanceof Error ? error.message : 'Unknown error occurred',
              data: null
            })
          }
        ]
      };
    }
  }

  async cancelEvent(args: any) {
    try {
      const { id, client_reference_id } = args;

      if (!id && !client_reference_id) {
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                ok: false,
                error: 'Either id or client_reference_id must be provided',
                data: null
              })
            }
          ]
        };
      }

      let deleted = false;

      if (id) {
        deleted = await this.database.deleteEvent(Number(id));
      } else if (client_reference_id) {
        deleted = await this.database.deleteByClientReferenceId(String(client_reference_id));
      }

      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              ok: true,
              data: { deleted },
              error: null
            })
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              ok: false,
              error: error instanceof Error ? error.message : 'Unknown error occurred',
              data: null
            })
          }
        ]
      };
    }
  }

  async listEvents(args: any) {
    try {
      const { start_date, end_date, limit } = args;

      const events = await this.database.listEvents(
        start_date,
        end_date,
        limit ? Number(limit) : undefined
      );

      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              ok: true,
              data: { events },
              error: null
            })
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              ok: false,
              error: error instanceof Error ? error.message : 'Unknown error occurred',
              data: null
            })
          }
        ]
      };
    }
  }
}