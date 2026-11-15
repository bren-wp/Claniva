package hr.brendigo.clanarina.data

import android.content.Context
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper
import java.util.UUID

data class Member(
    val id: String, val name: String, val group: String, val email: String,
    val active: Boolean, val dueCents: Long, val paidCents: Long
) {
    val balanceCents: Long get() = dueCents - paidCents
}
data class Totals(val members: Int, val chargedCents: Long, val paidCents: Long) {
    val outstandingCents: Long get() = (chargedCents - paidCents).coerceAtLeast(0)
}
class LocalStore(context: Context) : SQLiteOpenHelper(context, "clanarina.db", null, 1) {
    override fun onCreate(db: SQLiteDatabase) {
        db.execSQL("CREATE TABLE members (id TEXT PRIMARY KEY, name TEXT NOT NULL, group_name TEXT NOT NULL DEFAULT '', email TEXT NOT NULL DEFAULT '', active INTEGER NOT NULL DEFAULT 1)")
        db.execSQL("CREATE TABLE charges (id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id), amount_cents INTEGER NOT NULL CHECK(amount_cents > 0), note TEXT NOT NULL DEFAULT '', created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP)")
        db.execSQL("CREATE TABLE payments (id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id), amount_cents INTEGER NOT NULL CHECK(amount_cents > 0), note TEXT NOT NULL DEFAULT '', created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP)")
        db.execSQL("CREATE INDEX ix_charges_member ON charges(member_id)")
        db.execSQL("CREATE INDEX ix_payments_member ON payments(member_id)")
    }
    override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) {
        error("Missing schema migration from $oldVersion to $newVersion — never delete financial data")
    }
    override fun onConfigure(db: SQLiteDatabase) { db.setForeignKeyConstraintsEnabled(true) }
    @Synchronized fun listMembers(): List<Member> {
        val result = mutableListOf<Member>()
        readableDatabase.rawQuery(
            """SELECT m.id,m.name,m.group_name,m.email,m.active,
               COALESCE((SELECT SUM(amount_cents) FROM charges WHERE member_id=m.id),0),
               COALESCE((SELECT SUM(amount_cents) FROM payments WHERE member_id=m.id),0)
               FROM members m ORDER BY m.name COLLATE NOCASE""", null
        ).use { c -> while (c.moveToNext()) result.add(Member(
            c.getString(0),c.getString(1),c.getString(2),c.getString(3),
            c.getInt(4)==1,c.getLong(5),c.getLong(6)
        )) }
        return result
    }
    @Synchronized fun addMember(name:String,group:String,email:String) {
        val n=name.trim()
        require(n.length in 2..150) { "Ime mora imati 2–150 znakova." }
        writableDatabase.execSQL(
            "INSERT INTO members(id,name,group_name,email) VALUES(?,?,?,?)",
            arrayOf(UUID.randomUUID().toString(),n,group.trim().take(100),email.trim().take(254))
        )
    }
    @Synchronized fun addCharge(memberId:String,cents:Long) {
        require(cents in 1..10_000_000_000L)
        writableDatabase.execSQL(
            "INSERT INTO charges(id,member_id,amount_cents,note) VALUES(?,?,?,?)",
            arrayOf<Any>(UUID.randomUUID().toString(),memberId,cents,"Članarina")
        )
    }
    @Synchronized fun addPayment(memberId:String,cents:Long) {
        require(cents in 1..10_000_000_000L)
        writableDatabase.execSQL(
            "INSERT INTO payments(id,member_id,amount_cents,note) VALUES(?,?,?,?)",
            arrayOf<Any>(UUID.randomUUID().toString(),memberId,cents,"Uplata članarine")
        )
    }
    @Synchronized fun totals(): Totals {
        val all=listMembers()
        return Totals(all.size,all.sumOf{it.dueCents},all.sumOf{it.paidCents})
    }
}
