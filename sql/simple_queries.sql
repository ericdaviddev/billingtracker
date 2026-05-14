select *
from billing.payments p
where p.location_id IN
      (select l.location_id from billing.locations l where l.managed_by_platform = 'true');

select *
from billing.payments p
inner join billing.locations l on p.location_id = l.location_id
where l.managed_by_platform;

select g.guarantor_id, g.first_name, g.last_name, SUM(p.payment_amount) as total_posted_amount
from billing.guarantors g
INNER JOIN billing.payments p ON g.guarantor_id = p.guarantor_id
WHERE p.payment_status = 'posted'
group by g.guarantor_id;


select g.guarantor_id, g.first_name, g.last_name, SUM(p.payment_amount) as total_posted_amount
from billing.guarantors g
         INNER JOIN billing.payments p ON g.guarantor_id = p.guarantor_id
        INNER JOIN billing.locations l ON l.location_id = p.location_id
WHERE l.managed_by_platform = false
group by g.guarantor_id;


select distinct p.payment_status
from billing.payments p
where p.;