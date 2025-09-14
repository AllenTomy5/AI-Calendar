import sqlite3 from 'sqlite3';
export class CalendarDatabase {
    db = null;
    async initialize() {
        return new Promise((resolve, reject) => {
            this.db = new sqlite3.Database(':memory:', (err) => {
                if (err) {
                    reject(err);
                    return;
                }
                this.createTables()
                    .then(() => resolve())
                    .catch(reject);
            });
        });
    }
    async createTables() {
        if (!this.db)
            throw new Error('Database not initialized');
        const createEventTable = `
      CREATE TABLE IF NOT EXISTS events (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        title TEXT NOT NULL,
        start_time TEXT NOT NULL,
        end_time TEXT NOT NULL,
        timezone TEXT DEFAULT 'UTC',
        location TEXT,
        attendees TEXT, -- JSON array as string
        notes TEXT,
        client_reference_id TEXT UNIQUE,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
      )
    `;
        return new Promise((resolve, reject) => {
            this.db.run(createEventTable, (err) => {
                if (err)
                    reject(err);
                else
                    resolve();
            });
        });
    }
    async saveEvent(event) {
        if (!this.db)
            throw new Error('Database not initialized');
        const { title, start, end, timezone = 'UTC', location, attendees, notes, client_reference_id } = event;
        if (new Date(end) <= new Date(start)) {
            throw new Error('End time must be after start time');
        }
        const attendeesJson = attendees ? JSON.stringify(attendees) : null;
        if (client_reference_id) {
            const existingEvent = await this.findByClientReferenceId(client_reference_id);
            if (existingEvent) {
                return this.updateEventById(existingEvent.id, {
                    title,
                    start,
                    end,
                    timezone,
                    location,
                    attendees,
                    notes
                });
            }
        }
        const insertQuery = `
      INSERT INTO events (title, start_time, end_time, timezone, location, attendees, notes, client_reference_id)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    `;
        return new Promise((resolve, reject) => {
            this.db.run(insertQuery, [title, start, end, timezone, location, attendeesJson, notes, client_reference_id], function (err) {
                if (err) {
                    reject(err);
                    return;
                }
                resolve({
                    id: this.lastID,
                    title,
                    start,
                    end,
                    timezone,
                    location,
                    attendees,
                    notes,
                    client_reference_id
                });
            });
        });
    }
    async updateEventById(id, updates) {
        if (!this.db)
            throw new Error('Database not initialized');
        const existing = await this.findById(id);
        if (!existing) {
            throw new Error(`Event with ID ${id} not found`);
        }
        const fieldsToUpdate = [];
        const values = [];
        if (updates.title !== undefined) {
            fieldsToUpdate.push('title = ?');
            values.push(updates.title);
        }
        if (updates.start !== undefined) {
            fieldsToUpdate.push('start_time = ?');
            values.push(updates.start);
        }
        if (updates.end !== undefined) {
            fieldsToUpdate.push('end_time = ?');
            values.push(updates.end);
        }
        if (updates.timezone !== undefined) {
            fieldsToUpdate.push('timezone = ?');
            values.push(updates.timezone);
        }
        if (updates.location !== undefined) {
            fieldsToUpdate.push('location = ?');
            values.push(updates.location);
        }
        if (updates.attendees !== undefined) {
            fieldsToUpdate.push('attendees = ?');
            values.push(JSON.stringify(updates.attendees));
        }
        if (updates.notes !== undefined) {
            fieldsToUpdate.push('notes = ?');
            values.push(updates.notes);
        }
        fieldsToUpdate.push('updated_at = CURRENT_TIMESTAMP');
        values.push(id);
        const updateQuery = `
      UPDATE events 
      SET ${fieldsToUpdate.join(', ')} 
      WHERE id = ?
    `;
        return new Promise((resolve, reject) => {
            this.db.run(updateQuery, values, (err) => {
                if (err) {
                    reject(err);
                    return;
                }
                this.findById(id)
                    .then(updatedEvent => {
                    if (!updatedEvent) {
                        reject(new Error('Failed to retrieve updated event'));
                        return;
                    }
                    resolve(updatedEvent);
                })
                    .catch(reject);
            });
        });
    }
    async deleteEvent(id) {
        if (!this.db)
            throw new Error('Database not initialized');
        return new Promise((resolve, reject) => {
            this.db.run('DELETE FROM events WHERE id = ?', [id], function (err) {
                if (err) {
                    reject(err);
                    return;
                }
                resolve(this.changes > 0);
            });
        });
    }
    async deleteByClientReferenceId(clientReferenceId) {
        if (!this.db)
            throw new Error('Database not initialized');
        return new Promise((resolve, reject) => {
            this.db.run('DELETE FROM events WHERE client_reference_id = ?', [clientReferenceId], function (err) {
                if (err) {
                    reject(err);
                    return;
                }
                resolve(this.changes > 0);
            });
        });
    }
    async findById(id) {
        if (!this.db)
            throw new Error('Database not initialized');
        return new Promise((resolve, reject) => {
            this.db.get('SELECT * FROM events WHERE id = ?', [id], (err, row) => {
                if (err) {
                    reject(err);
                    return;
                }
                if (!row) {
                    resolve(null);
                    return;
                }
                resolve(this.mapRowToEvent(row));
            });
        });
    }
    async findByClientReferenceId(clientReferenceId) {
        if (!this.db)
            throw new Error('Database not initialized');
        return new Promise((resolve, reject) => {
            this.db.get('SELECT * FROM events WHERE client_reference_id = ?', [clientReferenceId], (err, row) => {
                if (err) {
                    reject(err);
                    return;
                }
                if (!row) {
                    resolve(null);
                    return;
                }
                resolve(this.mapRowToEvent(row));
            });
        });
    }
    async listEvents(startDate, endDate, limit) {
        if (!this.db)
            throw new Error('Database not initialized');
        let query = 'SELECT * FROM events';
        const conditions = [];
        const values = [];
        if (startDate) {
            conditions.push('start_time >= ?');
            values.push(startDate);
        }
        if (endDate) {
            conditions.push('end_time <= ?');
            values.push(endDate);
        }
        if (conditions.length > 0) {
            query += ' WHERE ' + conditions.join(' AND ');
        }
        query += ' ORDER BY start_time ASC';
        if (limit) {
            query += ' LIMIT ?';
            values.push(limit);
        }
        return new Promise((resolve, reject) => {
            this.db.all(query, values, (err, rows) => {
                if (err) {
                    reject(err);
                    return;
                }
                const events = rows.map(row => this.mapRowToEvent(row));
                resolve(events);
            });
        });
    }
    mapRowToEvent(row) {
        return {
            id: row.id,
            title: row.title,
            start: row.start_time,
            end: row.end_time,
            timezone: row.timezone,
            location: row.location,
            attendees: row.attendees ? JSON.parse(row.attendees) : [],
            notes: row.notes,
            client_reference_id: row.client_reference_id,
            created_at: row.created_at,
            updated_at: row.updated_at
        };
    }
}
//# sourceMappingURL=database.js.map