package hr.brendigo.clanarina.data

import android.content.Context
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper
import java.time.YearMonth
import java.util.UUID

data class Member(
    val id: String,
    val name: String,
    val group: String,
    val email: String,
    val active: Boolean,
    val dueCents: Long,
    val paidCents: Long
) {
    val balanceCents: Long get() = dueCents - paidCents
    val debtCents: Long get() = balanceCents.coerceAtLeast(0)
    val creditCents: Long get() = (-balanceCents).coerceAtLeast(0)
}

data class Totals(
    val members: Int, val chargedCents: Long, val paidCents: Long,
    val outstandingCents: Long, val creditCents: Long
)

data class LedgerEntry(
    val id: String,
    val memberId: String,
    val memberName: String,
    val amountCents: Long,
    val note: String,
    val extra: String,
    val createdAt: String,
    val kind: String
)

/** Offline-only SQLite store. All upgrades preserve members, charges, payments and their IDs. */
class LocalStore(context: Context) : SQLiteOpenHelper(context, "clanarina.db", null, 2) {
    override fun onConfigure(db: SQLiteDatabase) { db.setForeignKeyConstraintsEnabled(true) }

    override fun onCreate(db: SQLiteDatabase) {
        db.execSQL("""CREATE TABLE members (
            id TEXT PRIMARY KEY, name TEXT NOT NULL, group_name TEXT NOT NULL DEFAULT '',
            email TEXT NOT NULL DEFAULT '', active INTEGER NOT NULL DEFAULT 1
        )""")
        db.execSQL("""CREATE TABLE charges (
            id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id),
            amount_cents INTEGER NOT NULL CHECK(amount_cents > 0), note TEXT NOT NULL DEFAULT '',
            created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, period TEXT NOT NULL DEFAULT ''
        )""")
        db.execSQL("""CREATE TABLE payments (
            id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id),
            amount_cents INTEGER NOT NULL CHECK(amount_cents > 0), note TEXT NOT NULL DEFAULT '',
            created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, reference TEXT NOT NULL DEFAULT ''
        )""")
        createAuxiliary(db)
    }

    override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) {
        if (oldVersion < 2 && newVersion >= 2) {
            db.execSQL("ALTER TABLE charges ADD COLUMN period TEXT NOT NULL DEFAULT ''")
            db.execSQL("ALTER TABLE payments ADD COLUMN reference TEXT NOT NULL DEFAULT ''")
            createAuxiliary(db)
        }
        if (newVersion > 2) error("Unsupported future schema version")
    }

    private fun createAuxiliary(db: SQLiteDatabase) {
        db.execSQL("CREATE INDEX IF NOT EXISTS ix_charges_member ON charges(member_id)")
        db.execSQL("CREATE INDEX IF NOT EXISTS ix_payments_member ON payments(member_id)")
        db.execSQL("""CREATE UNIQUE INDEX IF NOT EXISTS ux_charge_period
            ON charges(member_id, period) WHERE period <> ''""")
        db.execSQL("""CREATE UNIQUE INDEX IF NOT EXISTS ux_payment_reference
            ON payments(member_id, reference) WHERE reference <> ''""")
        db.execSQL("""CREATE TABLE IF NOT EXISTS audit_events (
            id TEXT PRIMARY KEY, action TEXT NOT NULL, subject_id TEXT NOT NULL,
            created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
        )""")
    }

    private fun audit(db: SQLiteDatabase, action: String, subjectId: String) {
        db.execSQL("INSERT INTO audit_events(id,action,subject_id) VALUES(?,?,?)",
            arrayOf(UUID.randomUUID().toString(), action, subjectId))
    }
    private inline fun <T> write(block: (SQLiteDatabase) -> T): T {
        val db = writableDatabase
        db.beginTransaction()
        try {
            val result = block(db)
            db.setTransactionSuccessful()
            return result
        } finally {
            db.endTransaction()
        }
    }

    @Synchronized fun listMembers(): List<Member> {
        val result = mutableListOf<Member>()
        readableDatabase.rawQuery(
            """SELECT m.id,m.name,m.group_name,m.email,m.active,
               COALESCE((SELECT SUM(amount_cents) FROM charges WHERE member_id=m.id),0),
               COALESCE((SELECT SUM(amount_cents) FROM payments WHERE member_id=m.id),0)
               FROM members m ORDER BY m.name COLLATE NOCASE""", null
        ).use { c ->
            while (c.moveToNext()) result.add(Member(
                c.getString(0), c.getString(1), c.getString(2), c.getString(3),
                c.getInt(4) == 1, c.getLong(5), c.getLong(6)
            ))
        }
        return result
    }

    private fun validateMember(name: String, group: String, email: String) {
        require(name.trim().length in 2..150) { "Ime mora imati 2–150 znakova." }
        require(group.trim().length <= 100) { "Grupa smije imati najviše 100 znakova." }
        val e = email.trim()
        require(e.length <= 254 && (e.isBlank() || (e.contains("@") && e.substringAfter("@").contains(".")))) {
            "Provjerite e-mail adresu."
        }
    }

    @Synchronized fun addMember(name: String, group: String, email: String) {
        validateMember(name, group, email)
        val id = UUID.randomUUID().toString()
        write { db ->
            db.execSQL("INSERT INTO members(id,name,group_name,email) VALUES(?,?,?,?)",
                arrayOf(id, name.trim(), group.trim(), email.trim()))
            audit(db, "member.created", id)
        }
    }

    @Synchronized fun updateMember(id: String, name: String, group: String, email: String, active: Boolean) {
        validateMember(name, group, email)
        write { db ->
            val statement = db.compileStatement(
                "UPDATE members SET name=?,group_name=?,email=?,active=? WHERE id=?")
            statement.bindString(1, name.trim())
            statement.bindString(2, group.trim())
            statement.bindString(3, email.trim())
            statement.bindLong(4, if (active) 1 else 0)
            statement.bindString(5, id)
            require(statement.executeUpdateDelete() == 1) { "Član nije pronađen." }
            audit(db, "member.updated", id)
        }
    }

    @Synchronized fun addCharge(memberId: String, cents: Long, period: String = "") {
        require(cents in 1..10_000_000_000L) { "Iznos nije ispravan." }
        val p = period.trim()
        require(p.isEmpty() || runCatching { YearMonth.parse(p) }.isSuccess) {
            "Razdoblje unesite u obliku GGGG-MM."
        }
        write { db ->
            db.execSQL("INSERT INTO charges(id,member_id,amount_cents,note,period) VALUES(?,?,?,?,?)",
                arrayOf<Any>(UUID.randomUUID().toString(), memberId, cents, "Članarina", p))
            audit(db, "charge.created", memberId)
        }
    }

    @Synchronized fun addPayment(memberId: String, cents: Long, reference: String = "") {
        require(cents in 1..10_000_000_000L) { "Iznos nije ispravan." }
        val r = reference.trim()
        require(r.length <= 100) { "Referenca smije imati najviše 100 znakova." }
        write { db ->
            db.execSQL("INSERT INTO payments(id,member_id,amount_cents,note,reference) VALUES(?,?,?,?,?)",
                arrayOf<Any>(UUID.randomUUID().toString(), memberId, cents, "Evidentirana uplata", r))
            audit(db, "payment.created", memberId)
        }
    }

    @Synchronized fun ledger(kind: String, memberId: String? = null): List<LedgerEntry> {
        require(kind == "charge" || kind == "payment")
        val table = if (kind == "charge") "charges" else "payments"
        val extra = if (kind == "charge") "period" else "reference"
        val where = if (memberId == null) "" else "WHERE t.member_id = ?"
        val rows = mutableListOf<LedgerEntry>()
        readableDatabase.rawQuery(
            """SELECT t.id,t.member_id,m.name,t.amount_cents,t.note,t.$extra,t.created_at
               FROM $table t JOIN members m ON m.id=t.member_id $where
               ORDER BY t.created_at DESC,t.id DESC""",
            if (memberId == null) null else arrayOf(memberId)
        ).use { c ->
            while (c.moveToNext()) rows.add(LedgerEntry(
                c.getString(0),c.getString(1),c.getString(2),c.getLong(3),
                c.getString(4),c.getString(5),c.getString(6),kind
            ))
        }
        return rows
    }

    @Synchronized fun totals(): Totals {
        val all = listMembers()
        return Totals(
            all.size, all.sumOf { it.dueCents }, all.sumOf { it.paidCents },
            all.sumOf { it.debtCents }, all.sumOf { it.creditCents }
        )
    }
}
